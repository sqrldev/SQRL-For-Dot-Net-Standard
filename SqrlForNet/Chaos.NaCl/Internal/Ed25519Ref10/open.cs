using System;

namespace SqrlForNet.Chaos.NaCl.Internal.Ed25519Ref10
{
    internal static partial class Ed25519Operations
    {
        public static bool crypto_sign_verify(
            byte[] signature, int signatureOffset,
            byte[] message, int messageOffset, int messageLength,
            byte[] publicKey, int publicKeyOffset)
        {
            byte[] h;
            byte[] checkr = new byte[32];
            GroupElementP3 A;
            GroupElementP2 R;

            if ((signature[signatureOffset + 63] & 224) != 0) return false;
            if (GroupOperations.ge_frombytes_negate_vartime(out A, publicKey, publicKeyOffset) != 0)
                return false;

            var hasher = new Sha512();
            hasher.Update(signature, signatureOffset, 32);
            hasher.Update(publicKey, publicKeyOffset, 32);
            hasher.Update(message, messageOffset, messageLength);
            h = hasher.FinalizeHash();

            ScalarOperations.sc_reduce(h);

            var sm32 = new byte[32];//todo: remove allocation
            Array.Copy(signature, signatureOffset + 32, sm32, 0, 32);
            GroupOperations.ge_double_scalarmult_vartime(out R, h, ref A, sm32);
            GroupOperations.ge_tobytes(checkr, 0, ref R);
            var result = CryptoBytes.ConstantTimeEquals(checkr, 0, signature, signatureOffset, 32);
            CryptoBytes.Wipe(h);
            CryptoBytes.Wipe(checkr);
            return result;
        }
    }
}