import type { IconName } from '$lib/icons';

export interface UtilsNavTool {
	key: string;
	labelKey: string;
	descKey: string;
	href: string;
	icon: IconName;
}

export interface UtilsNavBlock {
	key: string;
	titleKey: string;
	badge?: string;
	tools: UtilsNavTool[];
}

export const utilsBlocks: UtilsNavBlock[] = [
	{
		key: 'signing',
		titleKey: 'utils.blocks.signing.title',
		badge: 'UA',
		tools: [
			{
				key: 'verify',
				labelKey: 'utils.verify.cardTitle',
				descKey: 'utils.verify.description',
				href: '/utils/verify',
				icon: 'checkCircle'
			},
			{
				key: 'sign',
				labelKey: 'utils.sign.cardTitle',
				descKey: 'utils.sign.description',
				href: '/utils/sign',
				icon: 'key'
			},
			{
				key: 'container',
				labelKey: 'utils.container.cardTitle',
				descKey: 'utils.container.description',
				href: '/utils/container',
				icon: 'checkCircle'
			}
		]
	}
];
