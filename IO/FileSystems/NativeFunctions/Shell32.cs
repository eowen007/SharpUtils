﻿/* Date: 13.9.2017, Time: 18:57 */
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using IllidanS4.SharpUtils.Com;

namespace IllidanS4.SharpUtils.IO.FileSystems
{
	partial class ShellFileSystem
	{
		static class Shell32
		{
			public static readonly PROPERTYKEY PKEY_Size = new PROPERTYKEY("B725F130-47EF-101A-A5F1-02608C9EEBAC", 12);
			public static readonly PROPERTYKEY PKEY_Title = new PROPERTYKEY("F29F85E0-4FF9-1068-AB91-08002B27B3D9", 2);
			public static readonly PROPERTYKEY PKEY_FileAttributes = new PROPERTYKEY("B725F130-47EF-101A-A5F1-02608C9EEBAC", 13);
			public static readonly PROPERTYKEY PKEY_DateModified = new PROPERTYKEY("B725F130-47EF-101A-A5F1-02608C9EEBAC", 14);
			public static readonly PROPERTYKEY PKEY_DateCreated = new PROPERTYKEY("B725F130-47EF-101A-A5F1-02608C9EEBAC", 15);
			public static readonly PROPERTYKEY PKEY_DateAccessed = new PROPERTYKEY("B725F130-47EF-101A-A5F1-02608C9EEBAC", 16);
			public static readonly PROPERTYKEY PKEY_Link_TargetParsingPath = new PROPERTYKEY("B9B4B3FC-2B51-4A42-B5D8-324146AFCF25", 2);
			public static readonly PROPERTYKEY PKEY_Link_TargetUrl = new PROPERTYKEY("5CBF2787-48CF-4208-B90E-EE5E5D420294", 2);
			public static readonly PROPERTYKEY PKEY_ParsingName = new PROPERTYKEY("28636AA6-953D-11D2-B5D6-00C04FD918D0", 24);
			public static readonly PROPERTYKEY PKEY_ParsingPath = new PROPERTYKEY("28636AA6-953D-11D2-B5D6-00C04FD918D0", 30);
			
			public static readonly Guid BHID_Stream = new Guid("1CEBB3AB-7C10-499a-A417-92CA16C4CB83");
			public static readonly Guid BHID_LinkTargetItem = new Guid("3981E228-F559-11D3-8E3A-00C04F6837D5");
			public static readonly Guid BHID_SFUIObject = new Guid("3981E225-F559-11D3-8E3A-00C04F6837D5");
			public static readonly Guid BHID_SFObject = new Guid("3981E224-F559-11D3-8E3A-00C04F6837D5");
			public static readonly Guid BHID_StorageEnum = new Guid("4621A4E3-F0D6-4773-8A9C-46E77B174840");
			
			public static readonly Guid FOLDERID_Desktop = new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");
			
			[DllImport("shell32.dll", CharSet=CharSet.Auto, SetLastError=true, EntryPoint="ShellExecuteEx")]
			static extern bool _ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);
			
			public static void ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo)
			{
				bool ok = _ShellExecuteEx(ref lpExecInfo);
				if(!ok) throw new Win32Exception();
			}
			
			[StructLayout(LayoutKind.Sequential)]
			public struct SHELLEXECUTEINFO
			{
				public static readonly int Size = Marshal.SizeOf(typeof(SHELLEXECUTEINFO));
				
				public int cbSize;
				public int fMask;
				public IntPtr hwnd;
				[MarshalAs(UnmanagedType.LPTStr)]
				public string lpVerb;
				[MarshalAs(UnmanagedType.LPTStr)]
				public string lpFile;
				[MarshalAs(UnmanagedType.LPTStr)]
				public string lpParameters;
				[MarshalAs(UnmanagedType.LPTStr)]
				public string lpDirectory;
				public int nShow;
				public IntPtr hInstApp;
				public IntPtr lpIDList;
				[MarshalAs(UnmanagedType.LPTStr)]
				public string lpClass;
				public IntPtr hkeyClass;
				public int dwHotKey;
				public IntPtr hIcon;
				public IntPtr hProcess;
			}
			
			[DllImport("shell32.dll", CharSet=CharSet.Unicode, PreserveSig=false)]
			[return: MarshalAs(UnmanagedType.IUnknown, IidParameterIndex=2)]
			static extern object SHCreateItemFromParsingName(string pszPath, IBindCtx pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid);
			
			[DebuggerStepThrough]
			public static T SHCreateItemFromParsingName<T>(string pszPath, IBindCtx pbc) where T : class
			{
				return (T)SHCreateItemFromParsingName(pszPath, pbc, typeof(T).GUID);
			}
			
			[DllImport("shell32.dll", PreserveSig=false)]
			[return: MarshalAs(UnmanagedType.IUnknown, IidParameterIndex=3)]
			static extern object SHBindToObject(IShellFolder psf, IntPtr pidl, IBindCtx pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid);
			
			[DebuggerStepThrough]
			public static T SHBindToObject<T>(IShellFolder psf, IntPtr pidl, IBindCtx pbc) where T : class
			{
				return (T)SHBindToObject(psf, pidl, pbc, typeof(T).GUID);
			}
			
			[DllImport("shell32.dll", CharSet=CharSet.Unicode, PreserveSig=false)]
			static extern void SHParseDisplayName(string pszName, IBindCtx pbc, out IntPtr ppidl, SFGAOF sfgaoIn, out SFGAOF psfgaoOut);
			
			[DebuggerStepThrough]
			public static IntPtr SHParseDisplayName(string pszName, IBindCtx pbc, SFGAOF sfgaoIn, out SFGAOF psfgaoOut)
			{
				IntPtr pidl;
				SHParseDisplayName(pszName, pbc, out pidl, sfgaoIn, out psfgaoOut);
				return pidl;
			}
			
			[DllImport("shell32.dll", PreserveSig=false)]
			[return: MarshalAs(UnmanagedType.IUnknown, IidParameterIndex=3)]
			static extern object SHCreateItemWithParent(IntPtr pidlParent, IShellFolder psfParent, IntPtr pidl, [MarshalAs(UnmanagedType.LPStruct)] Guid riid);
			
			[DebuggerStepThrough]
			public static T SHCreateItemWithParent<T>(IntPtr pidlParent, IShellFolder psfParent, IntPtr pidl) where T : class
			{
				return (T)SHCreateItemWithParent(pidlParent, psfParent, pidl, typeof(T).GUID);
			}
			
			[DllImport("shell32.dll", PreserveSig=false)]
			[return: MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 1)]
			static extern object SHCreateItemFromIDList(IntPtr pidl, [MarshalAs(UnmanagedType.LPStruct)] Guid riid);
			
			[DebuggerStepThrough]
			public static T SHCreateItemFromIDList<T>(IntPtr pidl) where T : class
			{
				return (T)SHCreateItemFromIDList(pidl, typeof(T).GUID);
			}
			
			[DllImport("shell32.dll", PreserveSig=false)]
			public static extern IntPtr SHGetIDListFromObject([MarshalAs(UnmanagedType.IUnknown)] object punk);
			
			[DllImport("shell32.dll", SetLastError=true)]
			public static extern IntPtr ILClone(IntPtr pidl);
			
			[DllImport("shell32.dll", CharSet=CharSet.Auto)]
			public static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);
			
			[DllImport("shell32.dll", CharSet=CharSet.Unicode, PreserveSig=false)]
			public static extern string SHGetNameFromIDList(IntPtr pidl, SIGDN sigdnName);
			
			[DllImport("shell32.dll", SetLastError=true)]
			public static extern IntPtr ILCloneFirst(IntPtr pidl);
			
			[DllImport("shell32.dll")]
			public static extern bool ILRemoveLastID(IntPtr pidl);
			
			[DllImport("shell32.dll")]
			public static extern IntPtr ILFindLastID(IntPtr pidl);
			
			[DllImport("shell32.dll", PreserveSig=false)]
			public static extern void ILSaveToStream(IStream pstm, IntPtr pidl);
			
			[DllImport("shell32.dll", PreserveSig=false)]
			public static extern IntPtr ILLoadFromStreamEx(IStream pstm);
			
			[DllImport("shell32.dll")]
			public static extern int ILGetSize(IntPtr pidl);
			
			[DllImport("shell32.dll")]
			public static extern IntPtr ILCombine(IntPtr pidl1, IntPtr pidl2);
			
			[DllImport("shell32.dll")]
			public static extern bool ILIsEqual(IntPtr pidl1, IntPtr pidl2);
			
			[DebuggerStepThrough]
			public static bool ILIsEmpty(IntPtr pidl)
			{
				return pidl == IntPtr.Zero || ILGetSize(pidl) == 0;
			}
			
			[DllImport("shell32.dll", PreserveSig=false)]
			[return: MarshalAs(UnmanagedType.IUnknown, IidParameterIndex=3)]
			static extern object SHGetKnownFolderItem([MarshalAs(UnmanagedType.LPStruct)]Guid rfid, int dwFlags, IntPtr hToken, [MarshalAs(UnmanagedType.LPStruct)]Guid riid);
			
			
			
			[DebuggerStepThrough]
			public static T SHGetKnownFolderItem<T>(Guid rfid, int dwFlags, IntPtr hToken) where T : class
			{
				return (T)SHGetKnownFolderItem(rfid, dwFlags, hToken, typeof(T).GUID);
			}
			
			[DebuggerStepThrough]
			public static IShellLink CreateShellLink()
			{
				return (IShellLink)new ShellLink();
			}
			
			[DebuggerStepThrough]
			public static IFileOperation CreateFileOperation()
			{
				return (IFileOperation)new FileOperation();
			}
			
		    [ComImport]
			[Guid("00021401-0000-0000-C000-000000000046")]
			private class ShellLink
			{
				
			}
			
		    [ComImport]
			[Guid("3AD05575-8857-4850-9277-11B85BDB8E09")]
			private class FileOperation
			{
				
			}
		}
	}
}
