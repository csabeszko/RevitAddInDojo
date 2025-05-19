namespace RevitAddInDojo.Extensions
{
    public static class PickElementsOptionFactory
    {
        public static CurrentDocumentOption CreateCurrentDocumentOption() => new CurrentDocumentOption();
        public static LinkDocumentOption CreateLinkDocumentOption() => new LinkDocumentOption();
        public static BothDocumentOption CreateBothDocumentOption() => new BothDocumentOption();

    }
}
