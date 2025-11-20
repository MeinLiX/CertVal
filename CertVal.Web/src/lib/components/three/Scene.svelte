<script lang="ts">
	import { T, useTask } from '@threlte/core';
	import { Float, Environment, Stars, useTexture } from '@threlte/extras';
	import { Spring } from 'svelte/motion';
	import { authUiState } from '$lib/stores/authUiState.svelte';
	import { theme } from '$lib/stores/theme.svelte';
	import * as THREE from 'three';
	import logoSrc from '$lib/assets/logo-white.svg';

	const camX = new Spring(0);
	const camY = new Spring(0);

	const map = useTexture(logoSrc);

	$effect(() => {
		camX.target = (authUiState.mouseX - 0.5) * 1;
		camY.target = -(authUiState.mouseY - 0.5) * 1;
	});

	let camera: THREE.PerspectiveCamera | undefined = $state();

	$effect(() => {
		if (camera) {
			camera.lookAt(0, 0, 0);
		}
	});

	const particleCount = 150;
	const tempObject = new THREE.Object3D();

	const particleData = Array.from({ length: particleCount }, () => {
		const position = new THREE.Vector3(
			(Math.random() - 0.5) * 25,
			(Math.random() - 0.5) * 25,
			(Math.random() - 0.5) * 10 - 5
		);
		return {
			initialPosition: position.clone(),
			position: position,
			rotation: new THREE.Euler(Math.random() * Math.PI, Math.random() * Math.PI, 0),
			scale: Math.random() * 0.2 + 0.1, // Slightly larger
			speed: Math.random() * 0.2 + 0.1, // Slower, for sine wave
			offset: Math.random() * 100, // Random phase
			rotationSpeed: (Math.random() - 0.5) * 0.01 // Slower rotation
		};
	});

	const color = new THREE.Color();

	const paletteLight = ['#4f46e5', '#10b981', '#f43f5e', '#8b5cf6'];
	const paletteDark = ['#ffffff', '#a5f3fc', '#c4b5fd', '#818cf8'];

	let mesh: THREE.InstancedMesh | undefined = $state();

	$effect(() => {
		if (mesh) {
			const palette = theme.current === 'dark' ? paletteDark : paletteLight;
			for (let i = 0; i < particleCount; i++) {
				color.set(palette[Math.floor(Math.random() * palette.length)]);
				mesh.setColorAt(i, color);
			}
			if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
		}
	});

	useTask(() => {
		if (!mesh) return;

		const time = performance.now() * 0.001;

		for (let i = 0; i < particleCount; i++) {
			const data = particleData[i];

			data.position.y = data.initialPosition.y + Math.sin(time * data.speed + data.offset) * 0.5;
			data.position.x =
				data.initialPosition.x + Math.cos(time * data.speed * 0.5 + data.offset) * 0.3;

			data.rotation.x += data.rotationSpeed;
			data.rotation.y += data.rotationSpeed;

			tempObject.position.copy(data.position);
			tempObject.rotation.copy(data.rotation);
			tempObject.scale.setScalar(data.scale);
			tempObject.updateMatrix();

			mesh.setMatrixAt(i, tempObject.matrix);
		}
		mesh.instanceMatrix.needsUpdate = true;
	});
</script>

<T.PerspectiveCamera
	makeDefault
	bind:ref={camera}
	position={[camX.current, camY.current, 12]}
	fov={45}
/>

<Environment />
<Stars speed={0.5} count={1000} factor={4} saturation={0} fade />

<Float speed={2} rotationIntensity={0.2} floatIntensity={0.5} floatingRange={[0, 0.5]}>
	{#await map then value}
		<T.Mesh position={[0, 3.5, 0]} scale={1.5}>
			<T.PlaneGeometry args={[1, 1]} />
			<T.MeshBasicMaterial
				map={value}
				transparent
				opacity={0.9}
				side={THREE.DoubleSide}
				toneMapped={false}
				color={theme.current === 'dark' ? 'white' : 'black'}
			/>
		</T.Mesh>
	{/await}
</Float>

<T.InstancedMesh bind:ref={mesh} args={[undefined, undefined, particleCount]}>
	<T.SphereGeometry args={[0.05]} />
	<T.MeshStandardMaterial
		toneMapped={false}
		emissiveIntensity={0.5}
		roughness={0.5}
		metalness={0.5}
	/>
</T.InstancedMesh>

<T.PointLight position={[-5, 5, -5]} intensity={1} color="#4f46e5" distance={20} decay={2} />
<T.PointLight position={[5, -5, -5]} intensity={1} color="#10b981" distance={20} decay={2} />

<T.AmbientLight intensity={0.2} />
