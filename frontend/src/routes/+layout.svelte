<script lang="ts">
    import "../app.css";
    import Sidebar from "$lib/components/Sidebar.svelte";
    import { page } from "$app/stores";

    let { children } = $props();
</script>

<div class="flex min-h-screen text-gray-100">
    <!-- Sidebar -->
    <Sidebar currentPath={$page.url.pathname} />

    <!-- Main Content Area -->
    <main class="flex-1 md:ml-64 relative z-10 transition-all duration-300">
        {@render children()}
    </main>
</div>
