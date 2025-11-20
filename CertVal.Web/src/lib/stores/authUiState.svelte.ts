class AuthUiState {
    mouseX = $state(0);
    mouseY = $state(0);
    isTyping = $state(false);
    isValid = $state(false);
    focusedField = $state<string | null>(null);

    setMouse(x: number, y: number) {
        this.mouseX = x;
        this.mouseY = y;
    }
}

export const authUiState = new AuthUiState();
