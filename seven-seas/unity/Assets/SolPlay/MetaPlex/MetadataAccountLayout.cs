



namespace Solnet.Metaplex
{
    internal class MetadataAccountLayout
    {
        internal const int MethodOffset = 0;

        internal const int nameOffset = 65;

        internal const int symbolOffset = 101;

        internal const int uriOffset = 115;

        internal const int feeBasisOffset = 319;

        internal const int creatorSwitchOffset = 320;//boolean either 0 or 1

        internal const int creatorsCountOffset = 321; //beginning of creator byte stream if creators exist

    }
}