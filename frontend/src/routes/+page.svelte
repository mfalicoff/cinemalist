<script lang="ts">
    import type { PageData } from "./$types";
    import FilmModal from "$lib/components/FilmModal.svelte";
    import type { Film } from "$lib/types/film";

    let { data }: { data: PageData } = $props();

    let history = $derived(data.history || []);
    let error = $derived(data.error);

    let selectedFilm: Film | null = $state(null);
    let isModalOpen = $state(false);
    let isLoadingFilm = $state(false);

    async function viewFilm(imdbId: string) {
        isLoadingFilm = true;
        console.log("Viewing film with IMDb ID:", imdbId);
        try {
            const response = await fetch(`/api/films/${imdbId}`);
            if (!response.ok) {
                throw new Error(`Failed to fetch film: ${response.statusText}`);
            }
            const film = await response.json();
            if (film) {
                selectedFilm = film;
                isModalOpen = true;
            } else {
                alert("Film not found in database");
            }
        } catch (error) {
            console.error("Error loading film:", error);
            alert("Error loading film details");
        } finally {
            isLoadingFilm = false;
        }
    }

    function closeModal() {
        isModalOpen = false;
        selectedFilm = null;
    }

    // Group history by source
    let groupedHistory = $derived.by(() => {
        const groups = new Map<string, Array<(typeof history)[number]>>();
        history.forEach((item) => {
            const source = item.source;
            if (!groups.has(source)) {
                groups.set(source, []);
            }
            groups.get(source)!.push(item);
        });
        // Sort each group by date (newest first)
        groups.forEach((items) => {
            items.sort(
                (a, b) =>
                    new Date(b.scrapeDate).getTime() -
                    new Date(a.scrapeDate).getTime(),
            );
        });
        return groups;
    });

    let expandedSources: Set<string> = $state(new Set());
    let expandedScrapes: Set<string> = $state(new Set());

    function toggleSource(source: string) {
        const newSet = new Set(expandedSources);
        if (newSet.has(source)) {
            newSet.delete(source);
        } else {
            newSet.add(source);
        }
        expandedSources = newSet;
    }

    function toggleScrape(id: string) {
        const newSet = new Set(expandedScrapes);
        if (newSet.has(id)) {
            newSet.delete(id);
        } else {
            newSet.add(id);
        }
        expandedScrapes = newSet;
    }

    function formatDate(dateString: string): string {
        const date = new Date(dateString);
        return date.toLocaleDateString("en-US", {
            year: "numeric",
            month: "long",
            day: "numeric",
            hour: "2-digit",
            minute: "2-digit",
        });
    }

    function formatShortDate(dateString: string): string {
        const date = new Date(dateString);
        return date.toLocaleDateString("en-US", {
            month: "short",
            day: "numeric",
            year: "numeric",
        });
    }
</script>

<svelte:head>
    <title>CinemaList - Scraper History</title>
</svelte:head>

<FilmModal film={selectedFilm} isOpen={isModalOpen} onClose={closeModal} />

<div class="min-h-screen bg-gradient-to-br from-primary-500 to-primary-700">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 pt-16 md:pt-8">
        <!-- Header -->
        <header class="text-center text-white mb-6 md:mb-12">
            <h1
                class="text-3xl sm:text-4xl md:text-5xl lg:text-6xl font-bold mb-2"
            >
                📊 Scraper History
            </h1>
            <p class="text-base sm:text-lg md:text-xl opacity-90">
                View scraping activity by source
            </p>
        </header>

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
        {#if history.length === 0 && !error}
            <div
                class="bg-white rounded-xl shadow-lg p-4 sm:p-6 md:p-8 text-center"
            >
                <p class="text-gray-800 text-base sm:text-lg font-medium mb-2">
                    No scraper history found in the database.
                </p>
                <p class="text-gray-600 text-sm sm:text-base">
                    Run the scraper to populate data!
                </p>
            </div>
        {:else if groupedHistory.size > 0}
            <!-- Grouped History List -->
            <div class="space-y-3 sm:space-y-4 md:space-y-6 mb-8">
                {#each [...groupedHistory.entries()] as [source, scrapes]}
                    <div
                        class="bg-white rounded-xl shadow-lg overflow-hidden transition-shadow duration-200 hover:shadow-xl"
                    >
                        <!-- Source Group Header -->
                        <div
                            class="p-3 sm:p-4 md:p-6 cursor-pointer hover:bg-gray-50 transition-colors duration-200 flex justify-between items-center border-b-2 border-primary-500"
                            role="button"
                            tabindex="0"
                            onclick={() => toggleSource(source)}
                            onkeydown={(e) =>
                                (e.key === "Enter" || e.key === " ") &&
                                toggleSource(source)}
                        >
                            <div class="flex-1 min-w-0">
                                <h2
                                    class="text-xl sm:text-2xl md:text-3xl font-bold text-gray-900 mb-1 sm:mb-2 truncate"
                                >
                                    {source}
                                </h2>
                                <p class="text-gray-600 text-sm sm:text-base">
                                    📊 {scrapes.length} scrape{scrapes.length !==
                                    1
                                        ? "s"
                                        : ""} recorded
                                </p>
                                <p
                                    class="text-gray-500 text-xs sm:text-sm mt-1"
                                >
                                    Latest: {formatShortDate(
                                        scrapes[0].scrapeDate,
                                    )}
                                </p>
                            </div>
                            <div
                                class="text-2xl sm:text-3xl md:text-4xl text-primary-500 transition-transform duration-200 ml-2 sm:ml-4 flex-shrink-0"
                            >
                                {expandedSources.has(source) ? "▼" : "▶"}
                            </div>
                        </div>

                        <!-- Scrapes List (Expandable) -->
                        {#if expandedSources.has(source)}
                            <div class="bg-gray-50 p-2 sm:p-3 md:p-4">
                                <div
                                    class="space-y-2 sm:space-y-3 md:space-y-4"
                                >
                                    {#each scrapes as scrape}
                                        <div
                                            class="bg-white rounded-lg shadow overflow-hidden"
                                        >
                                            <!-- Individual Scrape Header -->
                                            <div
                                                class="p-2 sm:p-3 md:p-4 cursor-pointer hover:bg-gray-50 transition-colors duration-200 flex justify-between items-center"
                                                role="button"
                                                tabindex="0"
                                                onclick={() =>
                                                    toggleScrape(
                                                        scrape.id || "",
                                                    )}
                                                onkeydown={(e) =>
                                                    (e.key === "Enter" ||
                                                        e.key === " ") &&
                                                    toggleScrape(
                                                        scrape.id || "",
                                                    )}
                                            >
                                                <div class="flex-1 min-w-0">
                                                    <p
                                                        class="text-gray-600 text-xs sm:text-sm mb-1"
                                                    >
                                                        📅 {formatDate(
                                                            scrape.scrapeDate,
                                                        )}
                                                    </p>
                                                    <p
                                                        class="text-gray-900 font-semibold text-sm sm:text-base"
                                                    >
                                                        🎥 {Object.keys(
                                                            scrape.moviesScraped,
                                                        ).length} movies scraped
                                                    </p>
                                                </div>
                                                <div
                                                    class="text-xl sm:text-2xl text-primary-500 transition-transform duration-200 ml-2 sm:ml-4 flex-shrink-0"
                                                >
                                                    {expandedScrapes.has(
                                                        scrape.id || "",
                                                    )
                                                        ? "▼"
                                                        : "▶"}
                                                </div>
                                            </div>

                                            <!-- Movies List (Expandable) -->
                                            {#if expandedScrapes.has(scrape.id || "")}
                                                <div
                                                    class="bg-gray-50 border-t border-gray-200 p-2 sm:p-3 md:p-4"
                                                >
                                                    <h4
                                                        class="text-sm sm:text-base font-semibold text-gray-800 mb-2 sm:mb-3"
                                                    >
                                                        Movies Scraped:
                                                    </h4>
                                                    <div
                                                        class="space-y-1.5 sm:space-y-2"
                                                    >
                                                        {#each Object.entries(scrape.moviesScraped) as [title, imdbId]}
                                                            <div
                                                                class="bg-white p-2 sm:p-3 rounded border-l-4 border-primary-500 flex flex-col sm:flex-row sm:justify-between sm:items-center gap-2 transition-transform duration-200 hover:translate-x-1"
                                                            >
                                                                <span
                                                                    class="text-gray-900 text-xs sm:text-sm flex-1 break-words"
                                                                >
                                                                    🎬 {title}
                                                                </span>
                                                                <div
                                                                    class="flex gap-2 flex-wrap"
                                                                >
                                                                    <button
                                                                        onclick={() =>
                                                                            viewFilm(
                                                                                imdbId,
                                                                            )}
                                                                        disabled={isLoadingFilm}
                                                                        class="inline-block bg-green-500 hover:bg-green-600 disabled:bg-gray-400 text-white font-semibold py-2 px-4 rounded transition-colors duration-200 text-xs sm:text-sm w-full sm:w-auto"
                                                                    >
                                                                        {isLoadingFilm
                                                                            ? "Loading..."
                                                                            : "View Film"}
                                                                    </button>
                                                                </div>
                                                            </div>
                                                        {/each}
                                                    </div>
                                                </div>
                                            {/if}
                                        </div>
                                    {/each}
                                </div>
                            </div>
                        {/if}
                    </div>
                {/each}
            </div>
        {/if}

        <!-- Footer -->
        <footer class="text-center text-white py-6 sm:py-8">
            <p class="text-sm sm:text-base md:text-lg font-medium">
                Total scrape records: {history.length}
                {#if groupedHistory.size > 0}
                    • Sources: {groupedHistory.size}
                {/if}
            </p>
        </footer>
    </div>
</div>
