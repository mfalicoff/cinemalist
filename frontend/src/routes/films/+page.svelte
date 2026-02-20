<script lang="ts">
    import type { PageData } from "./$types";
    import FilmModal from "$lib/components/FilmModal.svelte";
    import ErrorPanel from "$lib/components/ui/ErrorPanel.svelte";
    import EmptyState from "$lib/components/ui/EmptyState.svelte";
    import SearchBar from "$lib/components/ui/SearchBar.svelte";
    import FilmCard from "$lib/components/FilmCard.svelte";
    import type { Film } from "$lib/types/film";

    let { data, form }: { data: PageData; form: any } = $props();

    let films = $state(data.films || []);
    let error = $derived(data.error);

    let selectedFilm: Film | null = $state(null);
    let isModalOpen = $state(false);
    let searchQuery = $state("");

    // Sync films with data changes
    $effect(() => {
        films = data.films || [];
    });

    // Handle form action result for optimistic updates
    $effect(() => {
        if (form?.success && form.tmdbId) {
            // Optimistically update the film in the local state
            films = films.map((f) =>
                f.tmdbId === form.tmdbId ? { ...f, isInRadarr: true } : f,
            );

            // Update the selected film if it matches
            if (selectedFilm && selectedFilm.tmdbId === form.tmdbId) {
                selectedFilm = { ...selectedFilm, isInRadarr: true };
            }
        }
    });

    // Filter films based on search query
    let filteredFilms = $derived.by(() => {
        if (!searchQuery.trim()) {
            return films;
        }
        const query = searchQuery.toLowerCase();
        return films.filter(
            (film) =>
                film.title.toLowerCase().includes(query) ||
                film.country?.toLowerCase().includes(query) ||
                film.year?.includes(query),
        );
    });

    function viewFilm(film: Film) {
        selectedFilm = film;
        isModalOpen = true;
    }

    function closeModal() {
        isModalOpen = false;
        selectedFilm = null;
    }
</script>

<svelte:head>
    <title>CinemaList - Films</title>
</svelte:head>

<FilmModal film={selectedFilm} isOpen={isModalOpen} onClose={closeModal} />

<div class="animate-fade-in pb-12">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 pt-16 md:pt-8">
        <!-- Header -->
        <header class="text-center text-white mb-12 relative">
            <div
                class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-64 h-64 bg-primary-600/20 rounded-full blur-3xl -z-10"
            ></div>
            <h1
                class="text-5xl md:text-7xl font-bold mb-4 font-outfit tracking-tight filter drop-shadow-[0_0_15px_rgba(255,255,255,0.2)]"
            >
                <span class="text-primary-400">🎬</span> Films
            </h1>
            <p class="text-xl opacity-80 max-w-2xl mx-auto text-gray-300">
                Browse and manage your cinematic collection easily.
            </p>
        </header>

        <!-- Search Bar -->
        <SearchBar
            bind:value={searchQuery}
            placeholder="Search films by title, director, country, or year..."
        />

        <div class="mb-12 max-w-3xl mx-auto relative z-20">
            {#if searchQuery}
                <div class="flex justify-center mt-4 animate-fade-in">
                    <span
                        class="px-4 py-1.5 rounded-full bg-white/5 border border-white/10 text-primary-300 text-sm font-medium shadow-[0_0_10px_rgba(124,58,237,0.2)]"
                    >
                        {filteredFilms.length} result{filteredFilms.length !== 1
                            ? "s"
                            : ""} found
                    </span>
                </div>
            {/if}
        </div>

        <!-- Error State -->
        {#if error}
            <ErrorPanel
                errorMessage={error}
                hint="Verify that your backend API is online."
            />
        {/if}

        <!-- Empty State -->
        {#if filteredFilms.length === 0 && !error}
            <EmptyState
                icon={searchQuery ? "🙈" : "🎞️"}
                title={searchQuery ? "No Matches Found" : "Library is Empty"}
                description={searchQuery
                    ? "Try adjusting your search terms to find what you're looking for."
                    : "Run your scrapers to start discovering amazing cinema!"}
            />
        {:else if filteredFilms.length > 0}
            <!-- Films Grid -->
            <div
                class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4 sm:gap-6 lg:gap-8 mb-12"
            >
                {#each filteredFilms as film, i}
                    <FilmCard {film} index={i % 20} onClick={viewFilm} />
                {/each}
            </div>
        {/if}

        <!-- Footer -->
        {#if films.length > 0}
            <footer class="text-center py-8 border-t border-white/10">
                <div
                    class="inline-flex items-center gap-3 px-6 py-2 rounded-full glass-panel border border-white/5"
                >
                    <p class="text-sm font-medium text-gray-300">
                        Total Collection: <span class="text-white font-bold"
                            >{films.length}</span
                        > films
                    </p>
                </div>
            </footer>
        {/if}
    </div>
</div>
