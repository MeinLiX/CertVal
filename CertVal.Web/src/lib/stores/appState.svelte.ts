class AppState {
	isLoading = $state(true);

	setLoading(value: boolean) {
		this.isLoading = value;
	}
}

export const appState = new AppState();
