namespace BasicBasic.Indirect.Tokens
{
    public interface IToken
    {
        int TokenCode { get; }
        float NumValue { get; }
        string StrValue { get; }
    }
}