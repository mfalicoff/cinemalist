<script lang="ts">
    interface Props {
        currentPath: string;
    }

    let { currentPath }: Props = $props();

    let isMobileMenuOpen = $state(false);

    const navItems = [
        { path: "/", label: "Scraper History", icon: "📊" },
        { path: "/films", label: "Films", icon: "🎬" },
    ];

    function isActive(path: string): boolean {
        return currentPath === path;
    }

    function toggleMobileMenu() {
        isMobileMenuOpen = !isMobileMenuOpen;
    }

    function closeMobileMenu() {
        isMobileMenuOpen = false;
    }
</script>

<!-- Mobile Menu Button - Only visible on mobile -->
<button
    onclick={toggleMobileMenu}
    class="md:hidden fixed top-4 left-4 z-50 p-2 bg-primary-600 text-white rounded-lg shadow-lg hover:bg-primary-700 transition-colors"
    aria-label="Toggle menu"
>
    {#if isMobileMenuOpen}
        <!-- Close Icon -->
        <svg
            class="w-6 h-6"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
        >
            <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M6 18L18 6M6 6l12 12"
            />
        </svg>
    {:else}
        <!-- Hamburger Icon -->
        <svg
            class="w-6 h-6"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
        >
            <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M4 6h16M4 12h16M4 18h16"
            />
        </svg>
    {/if}
</button>

<!-- Mobile Overlay - Only visible when menu is open -->
{#if isMobileMenuOpen}
    <div
        class="md:hidden fixed inset-0 bg-black bg-opacity-50 z-40"
        onclick={closeMobileMenu}
        role="button"
        tabindex="0"
        onkeydown={(e) => e.key === "Escape" && closeMobileMenu()}
    ></div>
{/if}

<!-- Sidebar -->
<aside
    class="{isMobileMenuOpen
        ? 'translate-x-0'
        : '-translate-x-full'} md:translate-x-0 fixed left-0 top-0 h-screen w-64 bg-linear-to-b from-primary-600 to-primary-700 text-white shadow-xl z-40 transition-transform duration-300 ease-in-out"
>
    <!-- Logo/Title -->
    <div class="p-6 border-b border-white border-opacity-20">
        <h1 class="text-2xl font-bold">🎬 CinemaList</h1>
        <p class="text-sm text-white text-opacity-75 mt-1">Film Collection</p>
    </div>

    <!-- Navigation -->
    <nav class="p-4">
        <ul class="space-y-2">
            {#each navItems as item}
                <li>
                    <a
                        href={item.path}
                        onclick={closeMobileMenu}
                        class="flex items-center gap-3 px-4 py-3 rounded-lg transition-all duration-200 {isActive(
                            item.path,
                        )
                            ? 'bg-white text-black bg-opacity-20 font-semibold'
                            : 'hover:bg-white hover:text-black hover:bg-opacity-10'}"
                    >
                        <span class="text-2xl">{item.icon}</span>
                        <span class="text-base">{item.label}</span>
                    </a>
                </li>
            {/each}
        </ul>
    </nav>

    <!-- Footer -->
    <div
        class="absolute bottom-0 left-0 right-0 p-6 border-t border-white border-opacity-20"
    >
        <a
            href="http://localhost:5104/scalar/v1"
            target="_blank"
            rel="noopener noreferrer"
            class="block text-center text-sm text-white text-opacity-75 hover:text-opacity-100 transition-opacity"
        >
            📚 API Documentation
        </a>
    </div>
</aside>
