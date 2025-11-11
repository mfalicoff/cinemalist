<script lang="ts">
    import type { PageData } from "./$types";
    import FilmModal from "$lib/components/FilmModal.svelte";
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

<div class="min-h-screen bg-gradient-to-br from-primary-500 to-primary-700">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 pt-16 md:pt-8">
        <!-- Header -->
        <header class="text-center text-white mb-8">
            <h1 class="text-5xl md:text-6xl font-bold mb-2">🎬 Films</h1>
            <p class="text-xl opacity-90">Browse your film collection</p>
        </header>

        <!-- Search Bar -->
        <div class="mb-8 max-w-2xl mx-auto">
            <div class="relative">
                <input
                    type="text"
                    bind:value={searchQuery}
                    placeholder="Search films by title, director, country, or year..."
                    class="w-full px-6 py-4 text-lg rounded-xl shadow-lg focus:outline-none focus:ring-4 focus:ring-white focus:ring-opacity-50 transition-all"
                />
                {#if searchQuery}
                    <button
                        onclick={() => (searchQuery = "")}
                        class="absolute right-4 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
                        aria-label="Clear search"
                    >
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
                    </button>
                {/if}
            </div>
            {#if searchQuery}
                <p class="text-white text-sm mt-2 text-center">
                    Found {filteredFilms.length} film{filteredFilms.length !== 1
                        ? "s"
                        : ""}
                </p>
            {/if}
        </div>

        <!-- Error State -->
        {#if error}
            <div
                class="bg-white rounded-xl shadow-lg p-6 mb-8 border-l-4 border-red-500"
            >
                <p class="text-red-500 font-semibold text-lg mb-2">
                    ⚠️ {error}
                </p>
                <p class="text-gray-600 text-sm">
                    Make sure the backend API is running on port 5104
                </p>
            </div>
        {/if}

        <!-- Empty State -->
        {#if filteredFilms.length === 0 && !error}
            <div class="bg-white rounded-xl shadow-lg p-8 text-center">
                <p class="text-gray-800 text-lg font-medium mb-2">
                    {searchQuery
                        ? "No films match your search"
                        : "No films found in the database"}
                </p>
                <p class="text-gray-600">
                    {searchQuery
                        ? "Try a different search term"
                        : "Run the scraper to add films!"}
                </p>
            </div>
        {:else if filteredFilms.length > 0}
            <!-- Films Grid -->
            <div
                class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3 sm:gap-4 md:gap-6 mb-8"
            >
                {#each filteredFilms as film}
                    <div
                        class="bg-white rounded-xl shadow-lg overflow-hidden transition-all duration-200 hover:shadow-2xl hover:scale-105 cursor-pointer"
                        onclick={() => viewFilm(film)}
                        onkeydown={(e) =>
                            (e.key === "Enter" || e.key === " ") &&
                            viewFilm(film)}
                        role="button"
                        tabindex="0"
                    >
                        <!-- Poster -->
                        {#if film.posterUrl && film.posterUrl !== "N/A"}
                            <div class="aspect-[2/3] overflow-hidden">
                                <img
                                    src={film.posterUrl}
                                    alt={film.title}
                                    class="w-full h-full object-cover"
                                />
                            </div>
                        {:else}
                            <div
                                class="aspect-[2/3] bg-gradient-to-br from-primary-500 to-primary-700 flex items-center justify-center"
                            >
                                <span class="text-6xl">🎬</span>
                            </div>
                        {/if}

                        <!-- Info -->
                        <div class="p-2 sm:p-3 md:p-4">
                            <h3
                                class="font-bold text-gray-900 text-xs sm:text-sm md:text-base mb-1 sm:mb-2 line-clamp-2"
                                title={film.title}
                            >
                                {film.title}
                            </h3>
                            <div
                                class="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-2 text-xs text-gray-500"
                            >
                                {#if film.year}
                                    <span>📅 {film.year}</span>
                                {/if}
                                {#if film.country}
                                    <span>🌍 {film.country}</span>
                                {/if}
                            </div>
                            <div
                                class="text-xs text-gray-500 mt-1 hidden sm:block"
                            >
                                <span
                                    >{film.isInRadarr
                                        ? "In Radarr: ✅"
                                        : "In Radarr: ❌"}</span
                                >
                            </div>
                        </div>
                    </div>
                {/each}
            </div>
        {/if}

        <!-- Footer -->
        <footer class="text-center text-white py-8">
            <p class="text-lg font-medium">
                Total films: {films.length}
                {#if searchQuery && filteredFilms.length !== films.length}
                    • Showing: {filteredFilms.length}
                {/if}
            </p>
        </footer>
    </div>
</div>
