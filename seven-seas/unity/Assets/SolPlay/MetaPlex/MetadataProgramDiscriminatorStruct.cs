using System.Collections.Generic;

namespace Solnet.Metaplex
{
    internal static class MetadataProgramDiscriminatorStruct
    {
        /// <summary>
        /// Represents the user-friendly names for the instruction discriminator types for the <see cref="MetadataProgram"/>.
        /// </summary>
        internal static readonly Dictionary<Values, string> Names = new()
        {
            { Values.CreateMetadataAccount, "Create MetadataAccount" },
            { Values.CreateMetadataAccountV3, "Create MetadataAccountV3" },
            { Values.UpdateMetadataAccount, "Update MetadataAccount" },
            { Values.DeprecatedCreateMasterEdition, "Create MasterEdition (deprecated) " },
            { Values.DeprecatedMintNewEditionFromMasterEditionViaPrintingToken, "Mint new Edition from MasterEdition via PrintingToken (deprecated)" },
            { Values.UpdatePrimarySaleHappenedViaToken, "Update PrimarySaleHappened" },
            { Values.DeprecatedSetReservationList, "Set ReservationList (deprecated)" },
            { Values.DeprecatedCreateReservationList, "Create Reservation List (deprecated)" },
            { Values.SignMetadata, "Sign Metadata" },
            { Values.DeprecatedMintPrintingTokensViaToken, "Mint PrintingTokens via token (deprecated)" },
            { Values.DeprecatedMintPrintingTokens, "Mint PrintingTokens (deprecated)" },
            { Values.CreateMasterEdition, "Create MasterEdition" },
            { Values.MintNewEditionFromMasterEditionViaToken, "Mint new Edition from MasterEdition via token" },
            { Values.ConvertMasterEditionV1ToV2, "Convert Master Edition from V1 to V2" },
            { Values.MintNewEditionFromMasterEditionViaVaultProxy, "Mint new Edition from MasterEdition via VaultProxy" },
            { Values.PuffMetadata, "Puff metadata" }
        };

        /// <summary>
        /// Represents the instruction types for the <see cref="MetadataProgram"/>. Values are defined by the discriminator of each instruction labeled in Metaplex Docs.
        /// </summary>
        internal enum Values : byte
        {
            /// <summary>
            /// 
            /// </summary>
            CreateMetadataAccount = 0,

            /// <summary>
            /// 
            /// </summary>
            UpdateMetadataAccount = 1,

            /// <summary>
            ///
            /// </summary>
            DeprecatedCreateMasterEdition = 2,

            /// <summary>
            /// 
            /// </summary>
            DeprecatedMintNewEditionFromMasterEditionViaPrintingToken = 3,

            /// <summary>
            /// 
            /// </summary>
            UpdatePrimarySaleHappenedViaToken = 4,

            /// <summary>
            /// 
            /// </summary>
            DeprecatedSetReservationList = 5,

            /// <summary>
            /// 
            /// </summary>
            DeprecatedCreateReservationList = 6,

            /// <summary>
            /// 
            /// </summary>
            SignMetadata = 7,

            /// <summary>
            /// 
            /// </summary>
            DeprecatedMintPrintingTokensViaToken = 8,
            /// <summary>
            /// 
            /// </summary>
            DeprecatedMintPrintingTokens = 9,

            /// <summary>
            /// 
            /// </summary>
            CreateMasterEdition = 10,

            /// <summary>
            /// 
            /// </summary>
            MintNewEditionFromMasterEditionViaToken = 11,

            /// <summary>
            /// 
            /// </summary>
            ConvertMasterEditionV1ToV2 = 12,

            /// <summary>
            /// 
            /// </summary>
            MintNewEditionFromMasterEditionViaVaultProxy = 13,

            /// <summary>
            /// 
            /// </summary>
            PuffMetadata = 14,

            /// <summary>
            /// 
            /// </summary>
            CreateMetadataAccountV3 = 33
        }
    }
}