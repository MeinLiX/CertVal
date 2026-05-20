<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import logoUrl from '$lib/assets/favicon.svg?url';
	import { t } from '$lib/i18n';

	let heroEl = $state<HTMLElement | null>(null);
	let mouseX = $state(0);
	let mouseY = $state(0);
	let mounted = $state(false);
	let cursor = $state(true); // blinking cursor toggle

	onMount(() => {
		if (userSession.isAuthenticated) { goto('/dashboard'); return; }
		mounted = true;
		const id = setInterval(() => (cursor = !cursor), 530);
		return () => clearInterval(id);
	});

	function trackMouse(e: MouseEvent) {
		if (!heroEl) return;
		const r = heroEl.getBoundingClientRect();
		mouseX = e.clientX - r.left;
		mouseY = e.clientY - r.top;
	}

	const termLines = $derived([
		{ t: 'cmd',   s: '> certval verify invoice.pdf.p7s' },
		{ t: 'gap',   s: '' },
		{ t: 'ok',    s: t('home.term.ok1', language.current) },
		{ t: 'line',  s: '  ─────────────────────────────────' },
		{ t: 'field', s: '  Subject   ФОП Іваненко Іван Іванович' },
		{ t: 'field', s: '  Issuer    PrivatBank CA — Acsk2' },
		{ t: 'field', s: '  Signed    2026-05-19  14:32:01 UTC' },
		{ t: 'tsp',   s: t('home.term.tsp', language.current) },
		{ t: 'gap',   s: '' },
		{ t: 'ok',    s: t('home.term.ok2', language.current) },
	]);
</script>

<svelte:head>
	<title>CertVal — {t('home.pageTitle', language.current)}</title>
</svelte:head>

<!-- ═══ HERO ═══════════════════════════════════════════════════════════ -->
<section
	class="hero"
	bind:this={heroEl}
	onmousemove={trackMouse}
	role="banner"
	style="--mx:{mouseX}px;--my:{mouseY}px"
>
	<!-- layered backgrounds -->
	<div class="hero-grid" aria-hidden="true"></div>
	<div class="hero-vignette" aria-hidden="true"></div>
	<div class="hero-spotlight" aria-hidden="true"></div>
	<div class="hero-scanline" aria-hidden="true"></div>

	<div class="hero-inner" class:mounted>
		<!-- left column -->
		<div class="hero-left">
			<div class="eyebrow">
				<img src={logoUrl} alt="" class="eyebrow-logo" />
				<span class="eyebrow-text">CERTVAL <span class="eyebrow-sep">//</span> FUTURE PROOF SECURITY</span>
			</div>

			<h1 class="headline">
				{t('home.headlineLine1', language.current)}<br /><span class="accent-text">{t('home.headlineLine2', language.current)}</span> <span class="nano-tech">ONLINE</span>
			</h1>

			<p class="sub">{t('home.sub', language.current)}</p>

			<!-- CTAs -->
			<div class="ctas">
				<a href="/auth/login" class="btn-primary-glow">
					{t('home.ctaStart', language.current)}
					<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" aria-hidden="true"><path d="M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3"/></svg>
				</a>
				<a href="/utils" class="btn-cyber-outline">
					{t('home.ctaUtils', language.current)}
					<span class="crosshair"></span>
				</a>
			</div>
		</div>

		<!-- right column — terminal card -->
		<div class="term-wrap" aria-hidden="true">
			<div class="term-glow-container">
				<div class="term">
					<div class="term-bar">
						<div class="term-dots">
							<span class="term-dot red"></span>
							<span class="term-dot yellow"></span>
							<span class="term-dot green"></span>
						</div>
						<span class="term-title">certval_node ~ verify</span>
						<span class="term-status-badge">SECURE</span>
					</div>
					<div class="term-body">
						{#each termLines as line, i}
							<div class="term-line term-{line.t}" style="animation-delay:{0.4 + i * 0.07}s">
								{line.s}
							</div>
						{/each}
						<div class="term-line term-cursor" style="animation-delay:{0.4 + termLines.length * 0.07}s">
							<span class="cursor-blink" class:visible={cursor}>▋</span>
						</div>
					</div>
				</div>
			</div>
		</div>
	</div>
</section>

<!-- ═══ FEATURES ═══════════════════════════════════════════════════════ -->
<section class="features">
	<div class="features-header">
		<h2 class="section-title">
			<span class="title-glyph">■</span>
			{t('home.featuresTitle', language.current)}
		</h2>
		<p class="section-desc">
			{t('home.featuresDesc', language.current)}
		</p>
	</div>
	<div class="features-inner">
		<div class="feat">
			<span class="feat-num">01 / MONITOR</span>
			<h3 class="feat-title">{t('home.feat1.title', language.current)}</h3>
			<p class="feat-desc">{t('home.feat1.desc', language.current)}</p>
			<a href="/auth/login" class="feat-link">
				<span>{t('home.feat1.link', language.current)}</span>
				<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
			</a>
		</div>
		<div class="feat">
			<span class="feat-num">02 / DECRYPT</span>
			<h3 class="feat-title">{t('home.feat2.title', language.current)}</h3>
			<p class="feat-desc">{t('home.feat2.desc', language.current)}</p>
			<a href="/utils/verify" class="feat-link">
				<span>{t('home.feat2.link', language.current)}</span>
				<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
			</a>
		</div>
		<div class="feat">
			<span class="feat-num">03 / AUTHOR</span>
			<h3 class="feat-title">{t('home.feat3.title', language.current)}</h3>
			<p class="feat-desc">{t('home.feat3.desc', language.current)}</p>
			<a href="/utils/sign" class="feat-link">
				<span>{t('home.feat3.link', language.current)}</span>
				<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
			</a>
		</div>
		<div class="feat">
			<span class="feat-num">04 / STRUCT</span>
			<h3 class="feat-title">{t('home.feat4.title', language.current)}</h3>
			<p class="feat-desc">{t('home.feat4.desc', language.current)}</p>
			<a href="/utils/container" class="feat-link">
				<span>{t('home.feat4.link', language.current)}</span>
				<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
			</a>
		</div>
	</div>
</section>

<style>
	/* ── Hero ───────────────────────────────────────────────────────── */
	.hero {
		position: relative;
		overflow: hidden;
		background-color: #030307;
		display: flex;
		align-items: center;
		width: 100vw;
		margin-left: calc(50% - 50vw);
		margin-top: calc(-1 * var(--space-5, 1.25rem));
		padding: 4.5rem 0;
		border-bottom: 1px solid rgba(79, 70, 255, 0.15);
	}
	@media (min-width: 900px) {
		.hero {
			margin-top: calc(-1 * var(--space-6, 1.5rem));
			padding: 6.5rem 0;
		}
	}

	/* Dot grid */
	.hero-grid {
		position: absolute;
		inset: 0;
		background-image: radial-gradient(rgba(79, 70, 255, 0.15) 1px, transparent 1px);
		background-size: 24px 24px;
		pointer-events: none;
	}

	/* Vignette edges */
	.hero-vignette {
		position: absolute;
		inset: 0;
		background:
			radial-gradient(ellipse 100% 60% at 50% 100%, rgba(3,3,7,0.95), transparent),
			radial-gradient(ellipse 100% 40% at 50% 0%, rgba(3,3,7,0.7), transparent);
		pointer-events: none;
	}

	/* Cursor spotlight */
	.hero-spotlight {
		position: absolute;
		inset: 0;
		pointer-events: none;
		background: radial-gradient(
			600px circle at var(--mx, 50%) var(--my, 50%),
			rgba(79, 70, 255, 0.08),
			transparent 65%
		);
	}

	/* Scan line */
	@keyframes scan {
		0%   { top: -2%; }
		100% { top: 102%; }
	}
	.hero-scanline {
		position: absolute;
		left: 0; right: 0;
		height: 2px;
		pointer-events: none;
		background: linear-gradient(
			90deg,
			transparent 0%,
			rgba(79, 70, 255, 0.3) 15%,
			rgba(130, 200, 255, 0.5) 50%,
			rgba(79, 70, 255, 0.3) 85%,
			transparent 100%
		);
		opacity: 0.8;
		animation: scan 10s linear infinite;
	}

	/* Inner layout */
	.hero-inner {
		position: relative;
		z-index: 1;
		width: 100%;
		max-width: 1160px;
		margin: 0 auto;
		padding: 0 var(--container-pad, 1.5rem);
		display: grid;
		grid-template-columns: 1.1fr 0.9fr;
		gap: 4rem;
		align-items: center;
		opacity: 0;
		transform: translateY(15px);
		filter: blur(4px);
		transition: opacity 0.8s cubic-bezier(0.16,1,0.3,1),
		            transform 0.8s cubic-bezier(0.16,1,0.3,1),
		            filter 0.8s cubic-bezier(0.16,1,0.3,1);
	}
	.hero-inner.mounted {
		opacity: 1;
		transform: translateY(0);
		filter: blur(0);
	}

	/* ── Eyebrow ──────────────────────────────────────────────────── */
	.eyebrow {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		margin-bottom: 1.5rem;
		padding: 0.25rem 0.75rem;
		background: rgba(79, 70, 255, 0.07);
		border: 1px solid rgba(79, 70, 255, 0.2);
		border-radius: 4px;
	}
	.eyebrow-logo {
		width: 14px; height: 14px;
		filter: invert(1) sepia(1) saturate(5) hue-rotate(200deg);
	}
	.eyebrow-text {
		font-family: var(--font-mono, monospace);
		font-size: 0.65rem;
		letter-spacing: 0.15em;
		text-transform: uppercase;
		color: rgba(140, 150, 255, 0.9);
	}
	.eyebrow-sep { color: #8286ff; margin: 0 0.25rem; }

	/* ── Headline ─────────────────────────────────────────────────── */
	.headline {
		font-family: var(--font-sans, -apple-system, system-ui, sans-serif);
		font-size: clamp(2.5rem, 5vw, 4rem);
		font-weight: 800;
		line-height: 1.05;
		letter-spacing: -0.04em;
		color: #ffffff;
		margin: 0 0 1.25rem;
	}
	.accent-text {
		background: linear-gradient(135deg, #7b74ff 0%, #a5a1ff 50%, #ffffff 100%);
		-webkit-background-clip: text;
		background-clip: text;
		-webkit-text-fill-color: transparent;
	}
	.nano-tech {
		font-family: var(--font-mono, monospace);
		font-size: 0.8rem;
		font-weight: 700;
		letter-spacing: 0.2em;
		color: #4ade80;
		border: 1px solid rgba(74, 222, 128, 0.3);
		padding: 0.1rem 0.4rem;
		border-radius: 3px;
		vertical-align: middle;
		margin-left: 0.25rem;
		text-shadow: 0 0 8px rgba(74, 222, 128, 0.3);
	}

	/* ── Sub ──────────────────────────────────────────────────────── */
	.sub {
		font-size: 1rem;
		color: #a3a1b8;
		line-height: 1.6;
		max-width: 480px;
		margin: 0 0 2.25rem;
	}

	/* ── CTAs ─────────────────────────────────────────────────────── */
	.ctas {
		display: flex;
		flex-wrap: wrap;
		gap: 1rem;
		align-items: center;
	}
	
	.btn-primary-glow {
		display: inline-flex;
		align-items: center;
		gap: 0.6rem;
		padding: 0.8rem 1.8rem;
		background: linear-gradient(135deg, #4f46ff 0%, #3e35df 100%);
		color: #fff;
		border-radius: 4px;
		font-size: 0.875rem;
		font-weight: 600;
		letter-spacing: 0.03em;
		text-decoration: none;
		border: 1px solid rgba(255, 255, 255, 0.1);
		transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
		box-shadow: 0 4px 20px -2px rgba(79, 70, 255, 0.4);
	}
	.btn-primary-glow:hover {
		transform: translateY(-2px);
		box-shadow: 0 0 30px rgba(79, 70, 255, 0.6);
		border-color: rgba(255, 255, 255, 0.25);
	}
	.btn-primary-glow svg {
		transition: transform 0.25s ease;
	}
	.btn-primary-glow:hover svg {
		transform: translateX(4px);
	}

	.btn-cyber-outline {
		position: relative;
		display: inline-flex;
		align-items: center;
		padding: 0.8rem 1.8rem;
		color: #e0dff2;
		background: rgba(255, 255, 255, 0.03);
		border: 1px solid rgba(255, 255, 255, 0.08);
		border-radius: 4px;
		font-size: 0.875rem;
		font-weight: 600;
		letter-spacing: 0.03em;
		text-decoration: none;
		transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
		backdrop-filter: blur(8px);
	}
	.btn-cyber-outline:hover {
		color: #ffffff;
		border-color: rgba(130, 134, 255, 0.4);
		background: rgba(79, 70, 255, 0.08);
		box-shadow: 0 0 20px rgba(79, 70, 255, 0.15);
	}
	.btn-cyber-outline .crosshair {
		position: absolute;
		width: 4px; height: 4px;
		background: #8286ff;
		top: -1px; right: -1px;
		border-radius: 50%;
		opacity: 0;
		transition: opacity 0.25s;
	}
	.btn-cyber-outline:hover .crosshair {
		opacity: 1;
	}

	/* ── Terminal ─────────────────────────────────────────────────── */
	.term-wrap {
		display: flex;
		justify-content: flex-end;
	}
	.term-glow-container {
		position: relative;
		width: 100%;
		max-width: 440px;
	}
	.term-glow-container::before {
		content: '';
		position: absolute;
		inset: -1px;
		background: linear-gradient(135deg, rgba(79, 70, 255, 0.4), transparent, rgba(74, 222, 128, 0.2));
		border-radius: 12px;
		z-index: 0;
		pointer-events: none;
	}
	.term {
		position: relative;
		z-index: 1;
		width: 100%;
		background: rgba(10, 10, 18, 0.85);
		border: 1px solid rgba(255, 255, 255, 0.06);
		border-radius: 12px;
		overflow: hidden;
		backdrop-filter: blur(16px);
		box-shadow:
			0 24px 50px -12px rgba(0,0,0,0.8),
			0 0 40px -10px rgba(79,70,255,0.15);
	}
	.term-bar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0.75rem 1rem;
		background: rgba(255, 255, 255, 0.02);
		border-bottom: 1px solid rgba(255, 255, 255, 0.06);
	}
	.term-dots {
		display: flex;
		gap: 0.35rem;
	}
	.term-dot {
		width: 8px; height: 8px;
		border-radius: 50%;
		flex-shrink: 0;
	}
	.term-dot.red    { background: rgba(255, 95, 86, 0.6); }
	.term-dot.yellow { background: rgba(254, 188, 46, 0.6); }
	.term-dot.green  { background: rgba(40, 200, 64, 0.6); }
	
	.term-title {
		font-family: var(--font-mono, monospace);
		font-size: 0.65rem;
		color: rgba(255, 255, 255, 0.4);
		letter-spacing: 0.06em;
		text-transform: uppercase;
	}
	.term-status-badge {
		font-family: var(--font-mono, monospace);
		font-size: 0.6rem;
		font-weight: 700;
		color: #4ade80;
		background: rgba(74, 222, 128, 0.1);
		border: 1px solid rgba(74, 222, 128, 0.25);
		padding: 0.1rem 0.35rem;
		border-radius: 3px;
	}
	
	.term-body {
		padding: 1.25rem 1.25rem 1.5rem;
		font-family: var(--font-mono, monospace);
		font-size: 0.75rem;
		line-height: 1.8;
	}

	@keyframes termFadeIn {
		from { opacity: 0; transform: translateY(4px); }
		to   { opacity: 1; transform: translateY(0); }
	}
	.term-line {
		opacity: 0;
		animation: termFadeIn 0.3s cubic-bezier(0.16, 1, 0.3, 1) forwards;
	}
	.term-cmd   { color: #e0dff2; }
	.term-ok    { color: #4ade80; font-weight: 600; }
	.term-tsp   { color: #4ade80; text-shadow: 0 0 6px rgba(74,222,128,0.2); }
	.term-field { color: rgba(255,255,255,0.45); }
	.term-line  { color: rgba(255,255,255,0.25); white-space: pre; }
	.term-cursor { opacity: 1 !important; animation: none !important; }
	.cursor-blink { color: #8286ff; opacity: 0; transition: opacity 0.05s; }
	.cursor-blink.visible { opacity: 1; }

	/* ── Features ─────────────────────────────────────────────────── */
	.features {
		background: var(--color-bg, #fafaf7);
		border-top: 1px solid var(--color-border, #e5e3dc);
		padding: 5rem var(--container-pad, 1.5rem);
		width: 100vw;
		margin-left: calc(50% - 50vw);
	}
	
	.features-header {
		max-width: 1160px;
		margin: 0 auto 3.5rem;
		text-align: left;
	}
	.section-title {
		font-size: 1.25rem;
		font-weight: 700;
		letter-spacing: -0.02em;
		color: var(--color-text, #0a0a0a);
		margin: 0 0 0.5rem;
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}
	.title-glyph {
		color: var(--color-primary, #4f46ff);
		font-size: 0.8rem;
		vertical-align: middle;
	}
	.section-desc {
		font-size: 0.9375rem;
		color: var(--color-text-muted, #707070);
		max-width: 550px;
	}

	.features-inner {
		max-width: 1160px;
		margin: 0 auto;
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		border: 1px solid var(--color-border, #e5e3dc);
		background: var(--color-bg, #ffffff);
		border-radius: 8px;
		overflow: hidden;
		box-shadow: 0 4px 20px rgba(0, 0, 0, 0.02);
	}
	.feat {
		padding: 2.25rem 2rem;
		border-right: 1px solid var(--color-border, #e5e3dc);
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
		transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
		background: var(--color-bg, #ffffff);
	}
	.feat:last-child { border-right: 0; }
	
	.feat:hover {
		background: var(--color-surface, #f9f9f6);
		transform: translateY(-2px);
		box-shadow: inset 0 -4px 0 var(--color-primary, #4f46ff);
	}
	
	.feat-num {
		font-family: var(--font-mono, monospace);
		font-size: 0.65rem;
		font-weight: 700;
		letter-spacing: 0.15em;
		color: var(--color-text-muted, #8a8a8a);
		transition: color 0.2s ease;
	}
	.feat:hover .feat-num {
		color: var(--color-primary, #4f46ff);
	}
	
	.feat-title {
		font-size: 1.05rem;
		font-weight: 700;
		color: var(--color-text, #0a0a0a);
		margin: 0;
		letter-spacing: -0.01em;
	}
	.feat-desc {
		font-size: 0.825rem;
		color: var(--color-text-muted, #666666);
		line-height: 1.6;
		flex: 1;
		margin: 0;
	}
	.feat-link {
		display: inline-flex;
		align-items: center;
		gap: 0.35rem;
		font-size: 0.8rem;
		font-weight: 700;
		color: var(--color-text, #0a0a0a);
		text-decoration: none;
		margin-top: auto;
		letter-spacing: 0.05em;
		text-transform: uppercase;
		transition: all 0.2s ease;
	}
	.feat-link svg {
		transform: translateX(0);
		transition: transform 0.2s ease, color 0.15s ease;
	}
	.feat:hover .feat-link {
		color: var(--color-primary, #4f46ff);
	}
	.feat:hover .feat-link svg {
		transform: translateX(3px);
		color: var(--color-primary, #4f46ff);
	}

	/* ── Responsive ───────────────────────────────────────────────── */
	@media (max-width: 1024px) {
		.features-inner { grid-template-columns: 1fr 1fr; }
		.feat { border-right: 0; border-bottom: 1px solid var(--color-border, #e5e3dc); }
		.feat:nth-child(odd) { border-right: 1px solid var(--color-border, #e5e3dc); }
		.feat:nth-child(n+3) { border-bottom: 0; }
	}
	@media (max-width: 900px) {
		.hero-inner {
			grid-template-columns: 1fr;
			gap: 3.5rem;
			text-align: center;
		}
		.eyebrow { justify-content: center; }
		.sub { margin: 0 auto 2.25rem; }
		.ctas { justify-content: center; }
		.term-wrap { justify-content: center; }
		.term { max-width: 100%; }
	}
	@media (max-width: 560px) {
		.headline { font-size: 2.25rem; }
		.features-inner { grid-template-columns: 1fr; border-radius: 6px; }
		.feat { border-right: 0 !important; border-bottom: 1px solid var(--color-border, #e5e3dc) !important; }
		.feat:last-child { border-bottom: 0 !important; }
		.feat-link { font-size: 0.75rem; }
	}
</style>
