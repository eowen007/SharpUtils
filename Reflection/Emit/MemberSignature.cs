﻿/* Date: 4.1.2015, Time: 17:22 */
using System;
using System.Reflection.Emit;

namespace IllidanS4.SharpUtils.Reflection.Emit
{
	public abstract class MemberSignature : ISignatureElement
	{
		public MdSigCallingConvention SignatureType{get; private set;}
		
		public MemberSignature(MdSigCallingConvention sigType)
		{
			SignatureType = sigType;
		}
		
		protected virtual void AddSignature(SignatureHelper signature)
		{
			signature.AddData((byte)SignatureType);
		}
		
		void ISignatureElement.AddSignature(SignatureHelper signature)
		{
			this.AddSignature(signature);
		}
	}
}
