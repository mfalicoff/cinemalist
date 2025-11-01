<script lang="ts">
    import "../app.css";
    import Sidebar from "$lib/components/Sidebar.svelte";
    import { page } from "$app/stores";

    let { children } = $props();
</script>

<div class="flex min-h-screen bg-gray-100">
    <!-- Sidebar -->
    <Sidebar currentPath={$page.url.pathname} />

    <!-- Main Content Area -->
    <main class="flex-1 ml-64">
        {@render children()}
    </main>
</div>
