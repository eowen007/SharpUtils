﻿/* Date: 11.9.2017, Time: 10:33 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Web;
using IllidanS4.SharpUtils.Com;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace IllidanS4.SharpUtils.IO.FileSystems
{
	partial class ShellFileSystem
	{
		/// <summary>
		/// This class stores a handle to an item in the shell file system,
		/// using a pointer to its ITEMLIST (PIDL). The structure is copied
		/// on construction and is owned solely by the instance of this class.
		/// </summary>
		class ShellFileHandle : ResourceHandle, IPropertyProviderResource<PROPERTYKEY>
		{
			IntPtr pidl;
			ShellFileSystem fs;
			
			public ShellFileHandle(IntPtr pidl, ShellFileSystem fs) : this(pidl, fs, false)
			{
				
			}
			
			public ShellFileHandle(IShellItem item, ShellFileSystem fs) : this(Shell32.SHGetIDListFromObject(item), fs, true)
			{
				
			}
			
			private ShellFileHandle(IntPtr pidl, ShellFileSystem fs, bool own) : base(fs)
			{
				this.pidl = own ? pidl : Shell32.ILClone(pidl);
				this.fs = fs;
			}
			
			public ShellFileHandle(byte[] idl, ShellFileSystem fs) : this(LoadIdList(idl), fs, true)
			{
				
			}
			
			private static IntPtr LoadIdList(byte[] idl)
			{
				using(var buffer = new MemoryStream(idl))
				{
					return Shell32.ILLoadFromStreamEx(new StreamWrapper(buffer));
				}
			}
			
			public byte[] SaveIdList()
			{
				using(var buffer = new MemoryStream())
				{
					Shell32.ILSaveToStream(new StreamWrapper(buffer), pidl);
					return buffer.ToArray();
				}
			}
			
			private IShellItem GetItem()
			{
				return Shell32.SHCreateItemFromIDList<IShellItem>(pidl);
			}
			
			public override Uri Uri{
				get{
					return fs.GetShellUri(pidl, false);
				}
			}
			
			protected override Uri TargetUri{
				get{
					var target = TargetInfo;
					try{
						return target.Uri;
					}finally{
						var dispose = target as IDisposable;
						if(dispose != null) dispose.Dispose();
					}
				}
			}
			
			protected override ResourceInfo TargetInfo{
				get{
					var item = GetItem();
					try{
						var targ = fs.GetTargetItem(item);
						if(targ == null) return null;
						
						string uri = targ as string;
						if(uri != null)
						{
							return new ResourceInfo(new Uri(uri));
						}
						var target = (IShellItem)targ;
						try{
							IntPtr pidl = Shell32.SHGetIDListFromObject(target);
							return new ShellFileHandle(pidl, fs, true);
						}finally{
							//TODO: this might release twice the same object
							Marshal.FinalReleaseComObject(target);
						}
					}finally{
						Marshal.FinalReleaseComObject(item);
					}
				}
			}
			
			public override ResourceInfo Parent{
				get{
					IntPtr pidl = Shell32.ILClone(this.pidl);
					Shell32.ILRemoveLastID(pidl);
					return new ShellFileHandle(pidl, fs, true);
				}
			}
			
			protected override DateTime CreationTimeUtc{
				get{
					var item = (IShellItem2)GetItem();
					try{
						FILETIME ft = item.GetFileTime(Shell32.PKEY_DateCreated);
						return Win32FileSystem.GetDateTime(ft);
					}finally{
						Marshal.FinalReleaseComObject(item);
					}
				}
			}
			
			protected override DateTime LastAccessTimeUtc{
				get{
					var item = (IShellItem2)GetItem();
					try{
						FILETIME ft = item.GetFileTime(Shell32.PKEY_DateAccessed);
						return Win32FileSystem.GetDateTime(ft);
					}finally{
						Marshal.FinalReleaseComObject(item);
					}
				}
			}
			
			protected override DateTime LastWriteTimeUtc{
				get{
					var item = (IShellItem2)GetItem();
					try{
						FILETIME ft = item.GetFileTime(Shell32.PKEY_DateModified);
						return Win32FileSystem.GetDateTime(ft);
					}finally{
						Marshal.FinalReleaseComObject(item);
					}
				}
			}
			
			protected override long Length{
				get{
					var item = (IShellItem2)GetItem();
					try{
						return (long)item.GetUInt64(Shell32.PKEY_Size);
					}finally{
						Marshal.FinalReleaseComObject(item);
					}
				}
			}
			
			public override Stream GetStream(FileMode mode, FileAccess access)
			{
				var item = GetItem();
				try{
					var stream = item.BindToHandler<IStream>(null, Shell32.BHID_Stream);
					return new IStreamWrapper(stream);
				}finally{
					Marshal.FinalReleaseComObject(item);
				}
			}
			
			public override Process Execute()
			{
				throw new NotImplementedException();
			}
			
			public override List<ResourceInfo> GetResources()
			{
				var list = new List<ResourceInfo>();
				
				var psf = Shell32.SHBindToObject<IShellFolder>(null, pidl, null);
				try{
					IEnumIDList peidl = psf.EnumObjects(fs.OwnerHwnd, EnumConst);
					
					if(peidl == null) return list;
					try{
						while(true)
						{
							IntPtr pidl2;
							int num;
							peidl.Next(1, out pidl2, out num);
							if(num == 0) break;
							try{
								IntPtr pidl3 = Shell32.ILCombine(pidl, pidl2);
								list.Add(new ShellFileHandle(pidl3, fs, true));
							}finally{
								Marshal.FreeCoTaskMem(pidl2);
							}
						}
					}finally{
						Marshal.FinalReleaseComObject(peidl);
					}
				}finally{
					Marshal.FinalReleaseComObject(psf);
				}
				
				return list;
			}
			
			protected override void Dispose(bool disposing)
			{
				if(pidl != IntPtr.Zero)
				{
					Marshal.FreeCoTaskMem(pidl);
					pidl = IntPtr.Zero;
				}
			}
			
			protected override string ContentType{
				get{
					throw new NotImplementedException();
				}
			}
			
			protected override string LocalPath{
				get{
					return Shell32.SHGetNameFromIDList(pidl, SIGDN.SIGDN_DESKTOPABSOLUTEPARSING);
				}
			}
			
			protected override string DisplayPath{
				get{
					return Shell32.SHGetNameFromIDList(pidl, SIGDN.SIGDN_DESKTOPABSOLUTEEDITING);
				}
			}
			
			protected override FileAttributes Attributes{
				get{
					var item = (IShellItem2)GetItem();
					try{
						return (FileAttributes)item.GetUInt32(Shell32.PKEY_FileAttributes);
					}finally{
						Marshal.FinalReleaseComObject(item);
					}
				}
			}
		
			public T GetProperty<T>(PROPERTYKEY property)
			{
				var item = (IShellItem2)GetItem();
				try{
					return (T)Propsys.PropVariantToVariant(item.GetProperty(ref property));
				}finally{
					Marshal.FinalReleaseComObject(item);
				}
			}
			
			public void SetProperty<T>(PROPERTYKEY property, T value)
			{
				var item = GetItem();
				try{
					var propvar = Propsys.VariantToPropVariant(value);
					var properties = Propsys.PSCreatePropertyChangeArray<IPropertyChangeArray>(new[]{Shell32.PKEY_FileAttributes}, new[]{Propsys.PKA_FLAGS.PKA_SET}, new[]{propvar});
					
					var op = Shell32.CreateFileOperation();
					op.SetOwnerWindow(fs.OwnerHwnd);
					op.SetOperationFlags(0x0400 | 0x0004 | 0x0200 | 0x00100000);
					op.SetProperties(properties);
					op.ApplyPropertiesToItem(item);
					op.PerformOperations();
				}finally{
					Marshal.FinalReleaseComObject(item);
				}
			}
			
			public override int GetHashCode()
			{
				byte[] data = SaveIdList();
	            int hash = 17;
	            foreach(byte b in data)
	            {
	            	hash = unchecked(hash * 31 + b.GetHashCode());
	            }
	            return hash;
			}
			
			public override bool Equals(ResourceHandle other)
			{
				var handle = (ShellFileHandle)other;
				if(handle != null) return Shell32.ILIsEqual(pidl, handle.pidl);
				return false;
			}
		}
	}
}
