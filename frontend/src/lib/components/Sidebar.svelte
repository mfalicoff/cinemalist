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
    class="md:hidden fixed top-4 left-4 z-50 p-2 bg-glass-base backdrop-blur-md border border-white/10 text-white rounded-xl shadow-[0_0_15px_rgba(124,58,237,0.3)] hover:bg-glass-hover hover:border-white/20 transition-all duration-300"
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
        class="md:hidden fixed inset-0 bg-black/60 backdrop-blur-sm z-40 transition-opacity animate-fade-in"
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
        : '-translate-x-full'} md:translate-x-0 fixed left-0 top-0 h-screen w-64 glass-panel border-r border-white/10 z-40 transition-transform duration-300 ease-in-out flex flex-col"
>
    <!-- Logo/Title -->
    <div class="p-6 border-b border-white/10 relative overflow-hidden">
        <!-- Subtle glow effect behind logo -->
        <div class="absolute -top-10 -left-10 w-32 h-32 bg-primary-500/20 rounded-full blur-3xl"></div>
        <h1 class="text-2xl font-bold font-outfit tracking-tight text-white relative z-10 flex items-center gap-2">
            <span class="text-3xl filter drop-shadow-[0_0_8px_rgba(124,58,237,0.5)]">🎬</span> CinemaList
        </h1>
        <p class="text-sm text-gray-400 mt-1 relative z-10 uppercase tracking-widest font-semibold flex items-center gap-2">
            Film Collection
            <span class="w-2 h-2 rounded-full bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.6)] animate-pulse"></span>
        </p>
    </div>

    <!-- Navigation -->
    <nav class="p-4 flex-1">
        <ul class="space-y-2">
            {#each navItems as item}
                <li>
                    <a
                        href={item.path}
                        onclick={closeMobileMenu}
                        class="group flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-300 relative overflow-hidden {isActive(
                            item.path,
                        )
                            ? 'bg-primary-500/10 text-white font-semibold border border-primary-500/30'
                            : 'text-gray-300 hover:text-white hover:bg-glass-hover border border-transparent hover:border-white/10'}"
                    >
                        <!-- Active Background Gradient -->
                        {#if isActive(item.path)}
                            <div class="absolute inset-0 bg-gradient-to-r from-primary-600/20 to-transparent"></div>
                            <div class="absolute left-0 top-0 bottom-0 w-1 bg-primary-500 rounded-r-full shadow-[0_0_10px_rgba(124,58,237,0.8)]"></div>
                        {/if}
                        
                        <span class="text-2xl relative z-10 transition-transform duration-300 group-hover:scale-110 {isActive(item.path) ? 'filter drop-shadow-[0_0_8px_rgba(255,255,255,0.4)]' : ''}">{item.icon}</span>
                        <span class="text-base relative z-10">{item.label}</span>
                    </a>
                </li>
            {/each}
        </ul>
    </nav>

    <!-- Footer -->
    <div
        class="p-6 border-t border-white/10 mt-auto relative overflow-hidden"
    >
        <div class="absolute -bottom-10 -right-10 w-32 h-32 bg-primary-600/10 rounded-full blur-3xl"></div>
        <a
            href="http://localhost:5104/scalar/v1"
            target="_blank"
            rel="noopener noreferrer"
            class="block text-center text-sm font-medium text-gray-400 hover:text-white transition-all duration-300 hover:-translate-y-0.5 relative z-10 glass-panel-hover p-3 rounded-xl border border-transparent group"
        >
            <span class="inline-block transition-transform duration-300 group-hover:scale-110 mr-2">📚</span> API Documentation
        </a>
    </div>
</aside>
