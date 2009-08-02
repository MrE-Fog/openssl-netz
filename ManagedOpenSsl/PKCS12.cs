﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace OpenSSL
{
	public class PKCS12 : Base, IDisposable
	{
		#region PKCS12 structure

		[StructLayout(LayoutKind.Sequential)]
		struct _PKCS12
		{
			IntPtr version;     //ASN1_INTEGER *version;
			IntPtr mac;         //PKCS12_MAC_DATA *mac;
			IntPtr authsafes;   //PKCS7 *authsafes;
		}
		#endregion

		private CryptoKey privateKey;
		private X509Certificate certificate;
		private Stack<X509Certificate> caCertificates;

		public PKCS12(IntPtr ptr, bool takeOwnership)
			: base(ptr, takeOwnership)
		{
		}

		public PKCS12(BIO bio, string password)
			: base(Native.ExpectNonNull(Native.d2i_PKCS12_bio(bio.Handle, IntPtr.Zero)), true)
		{
			IntPtr cert;
			IntPtr pkey;
			IntPtr cacerts;

			// Parse the PKCS12 object and get privatekey, cert, cacerts if available
			Native.ExpectSuccess(Native.PKCS12_parse(this.ptr, password, out pkey, out cert, out cacerts));
			if (pkey != IntPtr.Zero)
			{
				privateKey = new CryptoKey(pkey, true);
			}
			if (cert != IntPtr.Zero)
			{
				certificate = new X509Certificate(cert, true);
				if (privateKey != null)
				{
					// We have a private key, assign it to the cert
					privateKey.AddRef();
					CryptoKey key = new CryptoKey(privateKey.Handle, true);
					certificate.PrivateKey = key;
				}
			}
			if (cacerts != IntPtr.Zero)
			{
				caCertificates = new Stack<X509Certificate>(cacerts, true);
			}
		}

		public X509Certificate Certificate
		{
			get
			{
				if (certificate != null)
				{
					X509Certificate cert = new X509Certificate(certificate.Handle, true);
					cert.AddRef();
					if (privateKey != null)
					{
						CryptoKey key = new CryptoKey(privateKey.Handle, true);
						key.AddRef();
						cert.PrivateKey = key;
					}
					return cert;
				}
				return null;
			}
		}

		public CryptoKey PrivateKey
		{
			get
			{
				if (privateKey != null)
				{
					CryptoKey key = new CryptoKey(privateKey.Handle, true);
					key.AddRef();
					return key;
				}
				return null;
			}
		}

		public Stack<X509Certificate> CACertificates
		{
			get { return caCertificates; }
		}

		protected override void OnDispose()
		{
			if (certificate != null)
			{
				certificate.Dispose();
				certificate = null;
			}
			if (privateKey != null)
			{
				privateKey.Dispose();
				privateKey = null;
			}
			if (caCertificates != null)
			{
				caCertificates.Dispose();
				caCertificates = null;
			}
			Native.PKCS12_free(this.ptr);
		}
	}
}
