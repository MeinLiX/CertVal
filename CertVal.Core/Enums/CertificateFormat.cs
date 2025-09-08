namespace CertVal.Core.Enums;

public enum CertificateFormat
{
    Unknown = 0,
    CER = 1,    // DER encoded
    CRT = 2,    // PEM encoded  
    PEM = 3,    // PEM encoded
    DER = 4,    // DER encoded
    P7B = 5,    // PKCS#7 bundle
    P7C = 6,    // PKCS#7 bundle
    PFX = 7,    // PKCS#12
    P12 = 8     // PKCS#12
}
