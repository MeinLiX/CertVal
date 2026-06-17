<script lang="ts">
	import { goto } from '$app/navigation';
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import Card from '$lib/components/ui/Card.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { IconName } from '$lib/icons';

	interface Tool {
		key: string;
		titleKey: string;
		descKey: string;
		href: string;
		icon: IconName;
	}

	interface ToolBlock {
		titleKey: string;
		subtitleKey: string;
		tools: Tool[];
		badge?: string;
	}

	const blocks: ToolBlock[] = [
		{
			titleKey: 'utils.blocks.signing.title',
			subtitleKey: 'utils.blocks.signing.subtitle',
			badge: 'UA',
			tools: [
				{
					key: 'verify',
					titleKey: 'utils.verify.cardTitle',
					descKey: 'utils.verify.cardDescription',
					href: '/utils/verify',
					icon: 'checkCircle'
				},
				{
					key: 'sign',
					titleKey: 'utils.sign.cardTitle',
					descKey: 'utils.sign.cardDescription',
					href: '/utils/sign',
					icon: 'key'
				},
				{
					key: 'container',
					titleKey: 'utils.container.cardTitle',
					descKey: 'utils.container.cardDescription',
					href: '/utils/container',
					icon: 'checkCircle'
				}
			]
		},
		{
			titleKey: 'utils.blocks.x509.title',
			subtitleKey: 'utils.blocks.x509.subtitle',
			tools: [
				{
					key: 'decode',
					titleKey: 'utils.x509.decode.cardTitle',
					descKey: 'utils.x509.decode.cardDescription',
					href: '/utils/decode',
					icon: 'document'
				},
				{
					key: 'generate',
					titleKey: 'utils.x509.generate.cardTitle',
					descKey: 'utils.x509.generate.cardDescription',
					href: '/utils/generate',
					icon: 'key'
				},
				{
					key: 'convert',
					titleKey: 'utils.x509.convert.cardTitle',
					descKey: 'utils.x509.convert.cardDescription',
					href: '/utils/convert',
					icon: 'settings'
				},
				{
					key: 'match',
					titleKey: 'utils.x509.match.cardTitle',
					descKey: 'utils.x509.match.cardDescription',
					href: '/utils/match',
					icon: 'checkCircle'
				}
			]
		}
	];
</script>

<svelte:head>
	<title>{t('utils.title', language.current)} – CertVal</title>
	<meta name="description" content={t('utils.subtitle', language.current)} />
</svelte:head>

<div class="utils" data-test-id="utils-landing">
	<PageHeader
		eyebrow={t('utils.eyebrow', language.current)}
		title={t('utils.title', language.current)}
	/>

	{#each blocks as block}
		<section class="utils__block">
			<div class="utils__block-head">
				<h2 class="utils__block-title">
				{t(block.titleKey, language.current)}
				{#if block.badge}<span class="badge-ua">{block.badge}</span>{/if}
			</h2>
				<p class="utils__block-subtitle">{t(block.subtitleKey, language.current)}</p>
			</div>
			<div class="utils__grid">
				{#each block.tools as tool}
					<Card clickable hover onclick={() => goto(tool.href)} data-test-id={`utils-tool-${tool.key}`}>
						<div class="tool">
							<div class="tool__icon">
								<Icon name={tool.icon} size="lg" />
							</div>
							<h3 class="tool__title">{t(tool.titleKey, language.current)}</h3>
							<p class="tool__desc">{t(tool.descKey, language.current)}</p>
							<span class="tool__cta">
								{t('utils.open', language.current)}
								<Icon name="rightArrow" size="sm" />
							</span>
						</div>
					</Card>
				{/each}
			</div>
		</section>
	{/each}
</div>

<style>
	.utils {
		display: flex;
		flex-direction: column;
		gap: var(--space-8);
	}
	.utils__notice {
		display: inline-flex;
		align-items: center;
		gap: var(--space-2);
		padding: var(--space-2) var(--space-3);
		background-color: var(--color-primary-light);
		color: var(--color-primary);
		border-radius: var(--radius-md);
		font-size: var(--text-sm);
		align-self: flex-start;
	}
	.utils__block {
		display: flex;
		flex-direction: column;
		gap: var(--space-5);
	}
	.utils__block-title {
		font-family: var(--font-display);
		font-size: 1.5rem;
		margin: 0;
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}
	.badge-ua {
		display: inline-flex;
		align-items: center;
		padding: 0.1rem 0.45rem;
		background: var(--color-primary, #4f46ff);
		color: #fff;
		border-radius: var(--radius-sm, 0.25rem);
		font-family: var(--font-mono, monospace);
		font-size: 0.65rem;
		font-weight: 700;
		letter-spacing: 0.06em;
		line-height: 1.5;
		flex-shrink: 0;
	}
	.utils__block-subtitle {
		color: var(--color-text-secondary);
		margin: 0;
	}
	.utils__grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
		gap: var(--space-5);
	}
	.tool {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
		padding: var(--space-4);
		min-height: 200px;
		transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
	}
	.tool__icon {
		width: 48px;
		height: 48px;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		background: rgba(79, 70, 255, 0.08);
		color: var(--color-primary);
		border: 1px solid rgba(79, 70, 255, 0.15);
		border-radius: var(--radius-md);
		margin-bottom: var(--space-1);
		transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
	}
	.tool__title {
		font-size: 1.15rem;
		font-weight: 700;
		color: var(--color-text);
		margin: 0;
	}
	.tool__desc {
		color: var(--color-text-secondary);
		font-size: 0.875rem;
		line-height: 1.6;
		margin: 0;
		flex: 1;
	}
	.tool__cta {
		display: inline-flex;
		align-items: center;
		gap: var(--space-2);
		color: var(--color-primary);
		font-weight: 700;
		font-size: 0.8rem;
		letter-spacing: 0.05em;
		text-transform: uppercase;
		margin-top: var(--space-3);
		transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
	}
	.tool__cta :global(svg) {
		transform: translateX(0);
		transition: transform 0.25s cubic-bezier(0.16, 1, 0.3, 1);
	}
	:global(.card:hover) .tool__icon {
		background: var(--color-primary);
		color: #ffffff;
		box-shadow: 0 0 15px rgba(79, 70, 255, 0.4);
		border-color: transparent;
	}
	:global(.card:hover) .tool__cta {
		color: var(--color-text);
		text-decoration: underline;
	}
	:global(.card:hover) .tool__cta :global(svg) {
		transform: translateX(4px);
	}
</style>
