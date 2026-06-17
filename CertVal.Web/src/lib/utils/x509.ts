/**
 * Browser-side X.509 / PKCS helpers for the free certificate utilities.
 *
 * Everything here runs entirely in the browser using the Web Crypto API via
 * @peculiar/x509 — no certificate, key, or CSR material is ever sent to the
 * server. The /utils pages that use this module set ssr=false.
 */
import * as x509 from '@peculiar/x509';

if (typeof globalThis.crypto !== 'undefined' && globalThis.crypto.subtle) {
	x509.cryptoProvider.set(globalThis.crypto);
}

const PEM_BLOCK = /-----BEGIN ([A-Z0-9 ]+)-----([\s\S]*?)-----END \1-----/g;

export interface PemBlock {
	label: string;
	der: Uint8Array;
}

/** Decode a base64 string (standard, no line breaks) into bytes. */
export function base64ToBytes(b64: string): Uint8Array {
	const clean = b64.replace(/[^A-Za-z0-9+/=]/g, '');
	const bin = atob(clean);
	const out = new Uint8Array(bin.length);
	for (let i = 0; i < bin.length; i++) out[i] = bin.charCodeAt(i);
	return out;
}

/** Encode bytes to standard base64. */
export function bytesToBase64(bytes: Uint8Array): string {
	let bin = '';
	for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
	return btoa(bin);
}

export function bytesToHex(bytes: Uint8Array, separator = ':'): string {
	return Array.from(bytes)
		.map((b) => b.toString(16).padStart(2, '0'))
		.join(separator);
}

/** Wrap DER bytes into a labelled PEM string (64-char lines). */
export function derToPem(der: Uint8Array, label: string): string {
	const b64 = bytesToBase64(der);
	const lines = b64.match(/.{1,64}/g) ?? [b64];
	return `-----BEGIN ${label}-----\n${lines.join('\n')}\n-----END ${label}-----\n`;
}

/** Extract every PEM block from a text blob. */
export function extractPemBlocks(text: string): PemBlock[] {
	const blocks: PemBlock[] = [];
	let m: RegExpExecArray | null;
	PEM_BLOCK.lastIndex = 0;
	while ((m = PEM_BLOCK.exec(text)) !== null) {
		blocks.push({ label: m[1].trim(), der: base64ToBytes(m[2]) });
	}
	return blocks;
}

export function isProbablyText(bytes: Uint8Array): boolean {
	const slice = bytes.subarray(0, Math.min(bytes.length, 64));
	return slice.every((b) => b === 9 || b === 10 || b === 13 || (b >= 32 && b < 127));
}

/** Convert an input (PEM text or raw DER bytes) into one or more cert DER blocks. */
export function toCertificateDers(input: Uint8Array): Uint8Array[] {
	if (isProbablyText(input)) {
		const text = new TextDecoder().decode(input);
		const blocks = extractPemBlocks(text).filter((b) => b.label.includes('CERTIFICATE'));
		if (blocks.length > 0) return blocks.map((b) => b.der);
	}
	// Assume single DER certificate.
	return [input];
}

export interface CertExtensionInfo {
	subjectAltNames: string[];
	keyUsages: string[];
	extendedKeyUsages: string[];
	isCa: boolean | null;
	pathLength: number | null;
	subjectKeyId: string | null;
	authorityKeyId: string | null;
	crlUrls: string[];
	ocspUrls: string[];
	issuerUrls: string[];
}

export interface CertInfo {
	subject: string;
	issuer: string;
	serialNumber: string;
	version: number;
	notBefore: string;
	notAfter: string;
	daysRemaining: number;
	isExpired: boolean;
	isNotYetValid: boolean;
	selfSigned: boolean;
	publicKeyAlgorithm: string;
	publicKeySize: string;
	signatureAlgorithm: string;
	sha1: string;
	sha256: string;
	extensions: CertExtensionInfo;
	pem: string;
}

function sigAlgName(alg: Algorithm | { name?: string; hash?: { name?: string } }): string {
	const a = alg as { name?: string; hash?: { name?: string } };
	const hash = a.hash?.name ? ` (${a.hash.name})` : '';
	return `${a.name ?? 'unknown'}${hash}`;
}

async function publicKeyDetails(
	cert: x509.X509Certificate
): Promise<{ algorithm: string; size: string }> {
	try {
		const key = await cert.publicKey.export();
		const alg = key.algorithm as RsaKeyAlgorithm & EcKeyAlgorithm;
		if (alg.name?.startsWith('RSA') || alg.name === 'RSASSA-PKCS1-v1_5' || alg.name === 'RSA-PSS') {
			return { algorithm: 'RSA', size: alg.modulusLength ? `${alg.modulusLength} bits` : '—' };
		}
		if (alg.name?.startsWith('EC') || alg.name === 'ECDSA' || alg.name === 'ECDH') {
			return { algorithm: 'EC', size: alg.namedCurve ?? '—' };
		}
		return { algorithm: alg.name ?? 'unknown', size: '—' };
	} catch {
		return { algorithm: cert.publicKey.algorithm.name ?? 'unknown', size: '—' };
	}
}

function readExtensions(cert: x509.X509Certificate): CertExtensionInfo {
	const info: CertExtensionInfo = {
		subjectAltNames: [],
		keyUsages: [],
		extendedKeyUsages: [],
		isCa: null,
		pathLength: null,
		subjectKeyId: null,
		authorityKeyId: null,
		crlUrls: [],
		ocspUrls: [],
		issuerUrls: []
	};

	const san = cert.getExtension(x509.SubjectAlternativeNameExtension);
	if (san) info.subjectAltNames = san.names.items.map((n) => `${n.type}: ${n.value}`);

	const ku = cert.getExtension(x509.KeyUsagesExtension);
	if (ku) {
		const flagNames = [
			'digitalSignature',
			'nonRepudiation',
			'keyEncipherment',
			'dataEncipherment',
			'keyAgreement',
			'keyCertSign',
			'cRLSign',
			'encipherOnly',
			'decipherOnly'
		] as const;
		for (const name of flagNames) {
			const value = x509.KeyUsageFlags[name] as number;
			if (value !== 0 && (ku.usages & value) === value) info.keyUsages.push(name);
		}
	}

	const eku = cert.getExtension(x509.ExtendedKeyUsageExtension);
	if (eku) info.extendedKeyUsages = eku.usages.map((u) => String(u));

	const bc = cert.getExtension(x509.BasicConstraintsExtension);
	if (bc) {
		info.isCa = bc.ca;
		info.pathLength = bc.pathLength ?? null;
	}

	const ski = cert.getExtension(x509.SubjectKeyIdentifierExtension);
	if (ski) info.subjectKeyId = ski.keyId;

	const aki = cert.getExtension(x509.AuthorityKeyIdentifierExtension);
	if (aki?.keyId) info.authorityKeyId = aki.keyId;

	return info;
}

export async function describeCertificate(der: Uint8Array): Promise<CertInfo> {
	const cert = new x509.X509Certificate(bytesToBase64(der));
	const now = Date.now();
	const notAfter = cert.notAfter.getTime();
	const notBefore = cert.notBefore.getTime();
	const sha1Buf = await cert.getThumbprint('SHA-1');
	const sha256Buf = await cert.getThumbprint('SHA-256');
	const pk = await publicKeyDetails(cert);

	return {
		subject: cert.subject,
		issuer: cert.issuer,
		serialNumber: cert.serialNumber,
		version: 3,
		notBefore: cert.notBefore.toISOString(),
		notAfter: cert.notAfter.toISOString(),
		daysRemaining: Math.floor((notAfter - now) / 86_400_000),
		isExpired: now > notAfter,
		isNotYetValid: now < notBefore,
		selfSigned: cert.subject === cert.issuer,
		publicKeyAlgorithm: pk.algorithm,
		publicKeySize: pk.size,
		signatureAlgorithm: sigAlgName(cert.signatureAlgorithm),
		sha1: bytesToHex(new Uint8Array(sha1Buf)),
		sha256: bytesToHex(new Uint8Array(sha256Buf)),
		extensions: readExtensions(cert),
		pem: cert.toString('pem') + '\n'
	};
}

/** Convert PEM text (single block) into DER bytes. */
export function pemToDer(pem: string): Uint8Array {
	const blocks = extractPemBlocks(pem);
	if (blocks.length > 0) return blocks[0].der;
	return base64ToBytes(pem);
}

/** Compare two SubjectPublicKeyInfo DER blobs for equality. */
export function publicKeysEqual(a: Uint8Array, b: Uint8Array): boolean {
	if (a.length !== b.length) return false;
	for (let i = 0; i < a.length; i++) if (a[i] !== b[i]) return false;
	return true;
}

export type KeyAlgo = 'RSA-2048' | 'RSA-3072' | 'RSA-4096' | 'EC-P256' | 'EC-P384';

export interface SubjectFields {
	CN?: string;
	O?: string;
	OU?: string;
	C?: string;
	L?: string;
	ST?: string;
}

export interface GenerateResult {
	privateKeyPem: string;
	publicKeyPem: string;
	csrPem?: string;
	certPem?: string;
	openssl: string;
}

function genParams(algo: KeyAlgo): {
	generate: RsaHashedKeyGenParams | EcKeyGenParams;
	sign: Algorithm;
} {
	switch (algo) {
		case 'RSA-2048':
		case 'RSA-3072':
		case 'RSA-4096': {
			const modulusLength = Number(algo.split('-')[1]);
			return {
				generate: {
					name: 'RSASSA-PKCS1-v1_5',
					hash: 'SHA-256',
					publicExponent: new Uint8Array([1, 0, 1]),
					modulusLength
				},
				sign: { name: 'RSASSA-PKCS1-v1_5', hash: 'SHA-256' } as Algorithm
			};
		}
		case 'EC-P256':
			return {
				generate: { name: 'ECDSA', namedCurve: 'P-256' },
				sign: { name: 'ECDSA', hash: 'SHA-256' } as Algorithm
			};
		case 'EC-P384':
			return {
				generate: { name: 'ECDSA', namedCurve: 'P-384' },
				sign: { name: 'ECDSA', hash: 'SHA-384' } as Algorithm
			};
	}
}

function buildDn(f: SubjectFields): string {
	const parts: string[] = [];
	if (f.CN) parts.push(`CN=${f.CN}`);
	if (f.O) parts.push(`O=${f.O}`);
	if (f.OU) parts.push(`OU=${f.OU}`);
	if (f.L) parts.push(`L=${f.L}`);
	if (f.ST) parts.push(`ST=${f.ST}`);
	if (f.C) parts.push(`C=${f.C}`);
	return parts.join(', ');
}

function opensslSubject(f: SubjectFields): string {
	const order: Array<[keyof SubjectFields, string]> = [
		['C', 'C'],
		['ST', 'ST'],
		['L', 'L'],
		['O', 'O'],
		['OU', 'OU'],
		['CN', 'CN']
	];
	return order
		.filter(([k]) => f[k])
		.map(([k, label]) => `/${label}=${f[k]}`)
		.join('');
}

async function exportKeyPemPair(keys: CryptoKeyPair): Promise<{ priv: string; pub: string }> {
	const pkcs8 = await globalThis.crypto.subtle.exportKey('pkcs8', keys.privateKey);
	const spki = await globalThis.crypto.subtle.exportKey('spki', keys.publicKey);
	return {
		priv: derToPem(new Uint8Array(pkcs8), 'PRIVATE KEY'),
		pub: derToPem(new Uint8Array(spki), 'PUBLIC KEY')
	};
}

function sanExtensions(sans: string[]): x509.Extension[] {
	const names = sans
		.map((s) => s.trim())
		.filter(Boolean)
		.map((value) => ({ type: 'dns' as const, value }));
	return names.length ? [new x509.SubjectAlternativeNameExtension(names)] : [];
}

/** Generate a key pair and a PKCS#10 CSR entirely in the browser. */
export async function generateCsr(
	algo: KeyAlgo,
	subject: SubjectFields,
	sans: string[]
): Promise<GenerateResult> {
	const { generate, sign } = genParams(algo);
	const keys = (await globalThis.crypto.subtle.generateKey(generate, true, [
		'sign',
		'verify'
	])) as CryptoKeyPair;

	const csr = await x509.Pkcs10CertificateRequestGenerator.create({
		name: buildDn(subject),
		keys,
		signingAlgorithm: sign,
		extensions: sanExtensions(sans)
	});

	const { priv, pub } = await exportKeyPemPair(keys);
	const isRsa = algo.startsWith('RSA');
	const keyOpt = isRsa
		? `rsa:${algo.split('-')[1]}`
		: `ec -pkeyopt ec_paramgen_curve:${algo === 'EC-P256' ? 'prime256v1' : 'secp384r1'}`;
	const openssl = `openssl req -new -newkey ${keyOpt} -nodes -keyout key.pem -out request.csr -subj "${opensslSubject(subject) || '/CN=example.com'}"`;

	return { privateKeyPem: priv, publicKeyPem: pub, csrPem: csr.toString('pem') + '\n', openssl };
}

/** Generate a key pair and a self-signed certificate entirely in the browser. */
export async function generateSelfSigned(
	algo: KeyAlgo,
	subject: SubjectFields,
	sans: string[],
	validityDays: number
): Promise<GenerateResult> {
	const { generate, sign } = genParams(algo);
	const keys = (await globalThis.crypto.subtle.generateKey(generate, true, [
		'sign',
		'verify'
	])) as CryptoKeyPair;

	const notBefore = new Date();
	const notAfter = new Date(notBefore.getTime() + validityDays * 86_400_000);
	const name = buildDn(subject) || 'CN=example.com';

	const cert = await x509.X509CertificateGenerator.createSelfSigned({
		serialNumber: bytesToHex(globalThis.crypto.getRandomValues(new Uint8Array(8)), ''),
		name,
		notBefore,
		notAfter,
		signingAlgorithm: sign,
		keys,
		extensions: [
			new x509.BasicConstraintsExtension(false, undefined, true),
			...sanExtensions(sans)
		]
	});

	const { priv, pub } = await exportKeyPemPair(keys);
	const isRsa = algo.startsWith('RSA');
	const keyOpt = isRsa
		? `rsa:${algo.split('-')[1]}`
		: `ec -pkeyopt ec_paramgen_curve:${algo === 'EC-P256' ? 'prime256v1' : 'secp384r1'}`;
	const openssl = `openssl req -x509 -newkey ${keyOpt} -nodes -keyout key.pem -out cert.pem -days ${validityDays} -subj "${opensslSubject(subject) || '/CN=example.com'}"`;

	return { privateKeyPem: priv, publicKeyPem: pub, certPem: cert.toString('pem') + '\n', openssl };
}

/** SubjectPublicKeyInfo DER from a certificate (PEM or DER). */
export function spkiFromCertificate(input: Uint8Array): Uint8Array {
	const der = toCertificateDers(input)[0];
	const cert = new x509.X509Certificate(bytesToBase64(der));
	return new Uint8Array(cert.publicKey.rawData);
}

/** SubjectPublicKeyInfo DER from a PKCS#10 CSR (PEM or DER). */
export function spkiFromCsr(input: Uint8Array): Uint8Array {
	let der = input;
	if (isProbablyText(input)) {
		const blocks = extractPemBlocks(new TextDecoder().decode(input)).filter((b) =>
			b.label.includes('REQUEST')
		);
		if (blocks.length) der = blocks[0].der;
	}
	const csr = new x509.Pkcs10CertificateRequest(bytesToBase64(der));
	return new Uint8Array(csr.publicKey.rawData);
}

/** SubjectPublicKeyInfo DER derived from a PKCS#8 private key (PEM or DER). */
export async function spkiFromPrivateKey(input: Uint8Array): Promise<Uint8Array> {
	let der = input;
	if (isProbablyText(input)) {
		const blocks = extractPemBlocks(new TextDecoder().decode(input));
		const pkcs8 = blocks.find((b) => b.label === 'PRIVATE KEY');
		if (!pkcs8) {
			throw new Error('Only PKCS#8 ("BEGIN PRIVATE KEY") keys are supported.');
		}
		der = pkcs8.der;
	}

	const candidates: Array<{ algo: Algorithm; usage: KeyUsage }> = [
		{ algo: { name: 'RSASSA-PKCS1-v1_5', hash: 'SHA-256' } as Algorithm, usage: 'sign' },
		{ algo: { name: 'RSA-PSS', hash: 'SHA-256' } as Algorithm, usage: 'sign' },
		{ algo: { name: 'ECDSA', namedCurve: 'P-256' } as Algorithm, usage: 'sign' },
		{ algo: { name: 'ECDSA', namedCurve: 'P-384' } as Algorithm, usage: 'sign' },
		{ algo: { name: 'ECDSA', namedCurve: 'P-521' } as Algorithm, usage: 'sign' }
	];

	for (const { algo, usage } of candidates) {
		try {
			const priv = await globalThis.crypto.subtle.importKey('pkcs8', der.slice(), algo, true, [
				usage
			]);
			const jwk = await globalThis.crypto.subtle.exportKey('jwk', priv);
			const pubJwk: JsonWebKey =
				jwk.kty === 'RSA'
					? { kty: 'RSA', n: jwk.n, e: jwk.e }
					: { kty: 'EC', crv: jwk.crv, x: jwk.x, y: jwk.y };
			const pub = await globalThis.crypto.subtle.importKey('jwk', pubJwk, algo, true, ['verify']);
			const spki = await globalThis.crypto.subtle.exportKey('spki', pub);
			return new Uint8Array(spki);
		} catch {
			/* try next candidate */
		}
	}
	throw new Error('Unsupported or invalid private key.');
}

