<script lang="ts">
    interface Props {
        currentPath: string;
    }

    let { currentPath }: Props = $props();

    const navItems = [
        { path: "/", label: "Scraper History", icon: "📊" },
        { path: "/films", label: "Films", icon: "🎬" },
    ];

    function isActive(path: string): boolean {
        return currentPath === path;
    }
</script>

<aside
    class="fixed left-0 top-0 h-screen w-64 bg-gradient-to-b from-primary-600 to-primary-700 text-white shadow-xl z-40"
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
                        class="flex items-center gap-3 px-4 py-3 rounded-lg transition-all duration-200 {isActive(
                            item.path,
                        )
                            ? 'bg-white bg-opacity-20 font-semibold'
                            : 'hover:bg-white hover:bg-opacity-10'}"
                    >
                        <span class="text-2xl">{item.icon}</span>
                        <span class="text-base">{item.label}</span>
                    </a>
                </li>
            {/each}
        </ul>
    </nav>

    <!-- Footer -->
    <div class="absolute bottom-0 left-0 right-0 p-6 border-t border-white border-opacity-20">
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
