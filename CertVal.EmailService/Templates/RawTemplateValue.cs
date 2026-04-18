namespace CertVal.EmailService.Templates;

public sealed record RawTemplateValue(string Value)
{
    public override string ToString() => Value;
}
