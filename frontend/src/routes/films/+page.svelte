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
        <div class="mb-12 max-w-3xl mx-auto relative z-20">
            <div class="relative group">
                <div
                    class="absolute inset-0 bg-primary-500/20 blur-xl rounded-full group-hover:bg-primary-500/30 transition-all duration-500"
                ></div>
                <div class="relative flex items-center">
                    <span class="absolute left-6 text-gray-400 text-xl">🔍</span
                    >
                    <input
                        type="text"
                        bind:value={searchQuery}
                        placeholder="Search films by title, director, country, or year..."
                        class="w-full pl-14 pr-16 py-5 text-lg rounded-2xl glass-input placeholder-gray-500 font-medium"
                    />
                    {#if searchQuery}
                        <button
                            onclick={() => (searchQuery = "")}
                            class="absolute right-6 text-gray-400 hover:text-white transition-colors bg-white/5 hover:bg-white/10 p-2 rounded-full"
                            aria-label="Clear search"
                        >
                            <svg
                                class="w-5 h-5"
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
            </div>
            {#if searchQuery}
                <div class="flex justify-center mt-4">
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
            <div
                class="glass-panel border-l-4 border-l-red-500 p-6 mb-8 rounded-xl animate-slide-up"
            >
                <div class="flex items-center gap-4">
                    <div class="p-3 bg-red-500/20 rounded-full">
                        <span
                            class="text-2xl filter drop-shadow-[0_0_8px_rgba(239,68,68,0.6)]"
                            >⚠️</span
                        >
                    </div>
                    <div>
                        <p class="text-red-400 font-semibold text-lg mb-1">
                            {error}
                        </p>
                        <p class="text-gray-400 text-sm">
                            Verify that your backend API is online.
                        </p>
                    </div>
                </div>
            </div>
        {/if}

        <!-- Empty State -->
        {#if filteredFilms.length === 0 && !error}
            <div
                class="glass-panel p-16 text-center rounded-3xl animate-slide-up border border-white/5 relative overflow-hidden"
            >
                <div
                    class="absolute inset-0 bg-gradient-to-b from-transparent to-black/50 z-0"
                ></div>
                <div class="relative z-10 block">
                    <div
                        class="w-24 h-24 mx-auto mb-6 bg-white/5 rounded-full flex items-center justify-center border border-white/10"
                    >
                        <span
                            class="text-5xl filter drop-shadow-[0_0_10px_rgba(255,255,255,0.2)]"
                        >
                            {searchQuery ? "🙈" : "🎞️"}
                        </span>
                    </div>
                    <h3 class="text-3xl font-bold text-white mb-3 font-outfit">
                        {searchQuery ? "No Matches Found" : "Library is Empty"}
                    </h3>
                    <p class="text-gray-400 text-lg max-w-md mx-auto">
                        {searchQuery
                            ? "Try adjusting your search terms to find what you're looking for."
                            : "Run your scrapers to start discovering amazing cinema!"}
                    </p>
                </div>
            </div>
        {:else if filteredFilms.length > 0}
            <!-- Films Grid -->
            <div
                class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4 sm:gap-6 lg:gap-8 mb-12"
            >
                {#each filteredFilms as film, i}
                    <div
                        class="group relative rounded-2xl overflow-hidden glass-panel-hover cursor-pointer border border-white/10 bg-black/40 animate-slide-up"
                        style="animation-delay: {(i % 20) * 50}ms"
                        onclick={() => viewFilm(film)}
                        onkeydown={(e) =>
                            (e.key === "Enter" || e.key === " ") &&
                            viewFilm(film)}
                        role="button"
                        tabindex="0"
                    >
                        <!-- Poster -->
                        <div
                            class="aspect-[2/3] w-full overflow-hidden relative bg-black"
                        >
                            {#if film.posterUrl && film.posterUrl !== "N/A"}
                                <img
                                    src={film.posterUrl}
                                    alt={film.title}
                                    class="w-full h-full object-cover transition-transform duration-700 group-hover:scale-110"
                                    loading="lazy"
                                />
                            {:else}
                                <div
                                    class="w-full h-full bg-gradient-to-br from-gray-900 to-black flex items-center justify-center border-b border-white/5"
                                >
                                    <span class="text-6xl opacity-50">🎬</span>
                                </div>
                            {/if}

                            <!-- Hover Overlay -->
                            <div
                                class="absolute inset-0 bg-gradient-to-t from-black via-black/50 to-transparent opacity-60 sm:opacity-0 sm:group-hover:opacity-100 transition-opacity duration-300 flex flex-col justify-end p-4"
                            >
                                <div
                                    class="translate-y-4 group-hover:translate-y-0 transition-transform duration-300"
                                >
                                    <button
                                        class="w-full btn-primary py-2 text-sm shadow-none"
                                        >View Details</button
                                    >
                                </div>
                            </div>

                            <!-- Radarr Status Badge -->
                            <div class="absolute top-3 right-3">
                                {#if film.isInRadarr}
                                    <div
                                        class="w-8 h-8 rounded-full bg-green-500/20 backdrop-blur-md border border-green-500/50 flex items-center justify-center shadow-[0_0_10px_rgba(34,197,94,0.4)]"
                                        title="In Radarr"
                                    >
                                        <span class="text-green-400 text-sm"
                                            >✓</span
                                        >
                                    </div>
                                {/if}
                            </div>
                        </div>

                        <!-- Info -->
                        <div class="p-4 relative">
                            <h3
                                class="font-bold text-white text-sm sm:text-base mb-2 line-clamp-1 font-outfit"
                                title={film.title}
                            >
                                {film.title}
                            </h3>
                            <div
                                class="flex items-center justify-between text-xs font-medium text-gray-400"
                            >
                                {#if film.year}
                                    <span
                                        class="px-2 py-1 bg-white/5 rounded-md border border-white/5"
                                        >{film.year}</span
                                    >
                                {/if}
                                {#if film.country}
                                    <span class="truncate ml-2"
                                        >{film.country}</span
                                    >
                                {/if}
                            </div>
                        </div>
                    </div>
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
