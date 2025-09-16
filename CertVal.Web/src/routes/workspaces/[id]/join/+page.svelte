<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	//let message = $state(t('workspaces.join.processing', $language));
	let message = $state(t('workspaces.join.success', $language));
	let isSuccess = $state(false);
	const workspaceId = $derived($page.params.id);

	onMount(async () => {
		isSuccess = true;
		//Temporarily disabled joining via token (TODO BACKEND)

		/*const token = $page.url.searchParams.get('token');
		if (!token) {
			message = t('workspaces.join.invalidToken', $language);
			return;
		}

		*/
	});
</script>

<svelte:head>
	<title>{t('workspaces.join.title', $language)}</title>
</svelte:head>

<div class="hero min-h-screen bg-base-200">
	<Card class="w-full max-w-md">
		<div class="py-12 text-center">
			<h3 class="text-xl font-semibold">{t('workspaces.join.title', $language)}</h3>
			<p class="mt-4">{message}</p>
			{#if isSuccess}
				<Button class="mt-6" onclick={() => goto(`/workspaces/${workspaceId}`)}>
					{t('workspaces.join.goToWorkspace', $language)}
				</Button>
			{/if}
		</div>
	</Card>
</div>
