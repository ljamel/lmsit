// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
	function shouldShowFirstLoginConfetti() {
		const body = document.body;
		if (!body) return false;

		const path = (window.location.pathname || '').toLowerCase();
		const isCoursesPage = path === '/courses';
		if (!isCoursesPage) return false;

		const isAuthenticated = body.dataset.authenticated === 'true';
		if (!isAuthenticated) return false;

		const user = (body.dataset.user || '').trim().toLowerCase();
		if (!user) return false;

		const storageKey = `ingenius:first-login-confetti:courses:${user}`;
		const alreadyShown = localStorage.getItem(storageKey) === '1';
		if (alreadyShown) return false;

		localStorage.setItem(storageKey, '1');
		return true;
	}

	function launchConfetti() {
		const colors = ['#00B2FF', '#FF5E00', '#00FF88', '#FFD166', '#FFFFFF'];
		const count = 140;
		const container = document.createElement('div');
		container.setAttribute('aria-hidden', 'true');
		container.style.position = 'fixed';
		container.style.left = '0';
		container.style.top = '0';
		container.style.width = '100%';
		container.style.height = '0';
		container.style.pointerEvents = 'none';
		container.style.zIndex = '9999';
		document.body.appendChild(container);

		for (let index = 0; index < count; index++) {
			const piece = document.createElement('span');
			const size = 6 + Math.random() * 8;
			const left = Math.random() * 100;
			const delay = Math.random() * 0.35;
			const duration = 1.9 + Math.random() * 2.1;
			const rotate = Math.random() * 900;
			const drift = (Math.random() - 0.5) * 240;

			piece.style.position = 'fixed';
			piece.style.top = '-20px';
			piece.style.left = `${left}vw`;
			piece.style.width = `${size}px`;
			piece.style.height = `${size * 0.6}px`;
			piece.style.background = colors[Math.floor(Math.random() * colors.length)];
			piece.style.opacity = '0.95';
			piece.style.borderRadius = Math.random() > 0.7 ? '50%' : '2px';
			piece.style.boxShadow = '0 0 10px rgba(255,255,255,0.35)';
			piece.style.willChange = 'transform, opacity';
			piece.style.animation = `ingenius-confetti-fall ${duration}s ease-out ${delay}s forwards`;
			piece.style.setProperty('--ingenius-drift', `${drift}px`);
			piece.style.setProperty('--ingenius-rotate', `${rotate}deg`);

			container.appendChild(piece);
		}

		setTimeout(function () {
			container.remove();
		}, 5000);
	}

	function ensureConfettiStyles() {
		if (document.getElementById('ingenius-confetti-style')) return;

		const style = document.createElement('style');
		style.id = 'ingenius-confetti-style';
		style.textContent = `
			@keyframes ingenius-confetti-fall {
				0% {
					transform: translate3d(0, -20px, 0) rotate(0deg);
					opacity: 0;
				}
				8% {
					opacity: 1;
				}
				100% {
					transform: translate3d(var(--ingenius-drift), 105vh, 0) rotate(var(--ingenius-rotate));
					opacity: 0;
				}
			}
		`;

		document.head.appendChild(style);
	}

	document.addEventListener('DOMContentLoaded', function () {
		if (!shouldShowFirstLoginConfetti()) return;
		ensureConfettiStyles();
		launchConfetti();
	});
})();


