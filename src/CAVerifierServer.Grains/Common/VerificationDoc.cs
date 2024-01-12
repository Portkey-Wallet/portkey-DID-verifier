using AElf.Types;

namespace CAVerifierServer.Grains.Common
{
    /// <summary>
    /// Base class for verification documents. Provides common properties and methods for verification documents.
    /// </summary>
    public abstract class VerificationDocBase
    {
        /// <summary>
        /// Gets the address associated with the verification document.
        /// </summary>
        protected Address Address { get; }

        /// <summary>
        /// Gets the guardian type associated with the verification document.
        /// </summary>
        protected int GuardianType { get; }

        /// <summary>
        /// Gets the salt value used in the verification document.
        /// </summary>
        protected string Salt { get; }

        /// <summary>
        /// Gets the hash of the guardian identifier associated with the verification document.
        /// </summary>
        protected string GuardianIdentifierHash { get; }

        /// <summary>
        /// Gets the operation type associated with the verification document.
        /// </summary>
        protected string OperationType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VerificationDocBase"/> class.
        /// </summary>
        /// <param name="address">The address associated with the verification document.</param>
        /// <param name="guardianType">The guardian type associated with the verification document.</param>
        /// <param name="salt">The salt value used in the verification document.</param>
        /// <param name="guardianIdentifierHash">The hash of the guardian identifier associated with the verification document.</param>
        /// <param name="operationType">The operation type associated with the verification document.</param>
        protected VerificationDocBase(Address address, int guardianType, string salt, string guardianIdentifierHash,
            string operationType)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            GuardianType = guardianType;
            Salt = salt ?? throw new ArgumentNullException(nameof(salt));
            GuardianIdentifierHash =
                guardianIdentifierHash ?? throw new ArgumentNullException(nameof(guardianIdentifierHash));
            OperationType = operationType ?? throw new ArgumentNullException(nameof(operationType));
        }

        /// <summary>
        /// Gets the string representation of the verification document.
        /// </summary>
        /// <returns>The string representation of the verification document.</returns>
        public abstract string GetStringRepresentation();
    }

    /// <summary>
    /// Represents a legacy verification document.
    /// </summary>
    public class LegacyVerificationDoc : VerificationDocBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LegacyVerificationDoc"/> class.
        /// </summary>
        public LegacyVerificationDoc(Address address, int guardianType, string salt, string guardianIdentifierHash,
            string operationType)
            : base(address, guardianType, salt, guardianIdentifierHash, operationType)
        {
        }

        /// <inheritdoc />
        public override string GetStringRepresentation()
        {
            return
                $"{GuardianType},{GuardianIdentifierHash},{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss.fff},{Address.ToBase58()},{Salt},{OperationType}";
        }
    }

    /// <summary>
    /// Represents a verification document.
    /// </summary>
    public class VerificationDoc : VerificationDocBase
    {
        /// <summary>
        /// The chain id of the network where the verify account is located.
        /// </summary>
        private string ChainId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VerificationDoc"/> class.
        /// </summary>
        public VerificationDoc(Address address, int guardianType, string salt, string guardianIdentifierHash,
            string operationType, string chainId)
            : base(address, guardianType, salt, guardianIdentifierHash, operationType)
        {
            ChainId = chainId ?? throw new ArgumentNullException(nameof(chainId));
        }

        /// <inheritdoc />
        public override string GetStringRepresentation()
        {
            return
                $"{GuardianType},{GuardianIdentifierHash},{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss.fff},{Address.ToBase58()},{Salt},{OperationType},{ChainId}";
        }
    }

    /// <summary>
    /// Factory class for creating instances of <see cref="VerificationDocBase"/> based on operation type.
    /// </summary>
    public static class VerificationDocFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="VerificationDocBase"/> based on the specified parameters.
        /// </summary>
        /// <param name="address">The address associated with the verification document.</param>
        /// <param name="guardianType">The guardian type associated with the verification document.</param>
        /// <param name="salt">The salt value used in the verification document.</param>
        /// <param name="guardianIdentifierHash">The hash of the guardian identifier associated with the verification document.</param>
        /// <param name="operationType">The operation type associated with the verification document.</param>
        /// <param name="chainId">The chain id of the network where the verify account is located.</param>
        /// <returns>An instance of <see cref="VerificationDocBase"/>.</returns>
        public static VerificationDocBase Create(Address address, int guardianType, string salt,
            string guardianIdentifierHash, string operationType, string chainId)
        {
            var isLegacy = string.IsNullOrWhiteSpace(chainId);
            if (isLegacy)
            {
                return new LegacyVerificationDoc(address, guardianType, salt, guardianIdentifierHash, operationType);
            }
            else
            {
                return new VerificationDoc(address, guardianType, salt, guardianIdentifierHash, operationType, chainId);
            }
        }
    }
}