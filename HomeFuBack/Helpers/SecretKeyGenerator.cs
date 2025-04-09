using System;
using System.Security.Cryptography;

namespace HomeFuBack.Helpers
{
	public static class SecretKeyGenerator
	{
		public static string GenerateSecretKey(int keySize = 256)
		{
			using (var rng = RandomNumberGenerator.Create())
			{
				byte[] keyBytes = new byte[keySize / 8];
				rng.GetBytes(keyBytes);
				return Convert.ToBase64String(keyBytes);
			}
		}
	}
}