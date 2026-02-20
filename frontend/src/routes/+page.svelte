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

<div class="animate-fade-in pb-12">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 pt-16 md:pt-8">
        <!-- Header -->
        <header class="text-center text-white mb-8 md:mb-16 relative">
            <div
                class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-64 h-64 bg-primary-500/20 rounded-full blur-3xl -z-10"
            ></div>
            <h1
                class="text-4xl sm:text-5xl md:text-6xl font-bold mb-4 font-outfit tracking-tight filter drop-shadow-[0_0_15px_rgba(255,255,255,0.2)]"
            >
                <span class="text-primary-400">📊 Scraper</span> History
            </h1>
            <p class="text-lg md:text-xl text-gray-400 max-w-2xl mx-auto">
                Track your automated film discovery across all sources
            </p>
        </header>

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
                            Make sure the backend API is running on port 5104
                        </p>
                    </div>
                </div>
            </div>
        {/if}

        <!-- Empty State -->
        {#if history.length === 0 && !error}
            <div
                class="glass-panel p-12 text-center rounded-2xl animate-slide-up border border-white/5"
            >
                <div
                    class="w-24 h-24 mx-auto mb-6 bg-primary-500/10 rounded-full flex items-center justify-center"
                >
                    <span
                        class="text-4xl filter drop-shadow-[0_0_10px_rgba(124,58,237,0.5)]"
                        >🔍</span
                    >
                </div>
                <h3 class="text-2xl font-bold text-white mb-2 font-outfit">
                    No History Found
                </h3>
                <p class="text-gray-400 max-w-md mx-auto">
                    Your database is currently empty. Run the scraper to start
                    discovering and tracking new premium films!
                </p>
            </div>
        {:else if groupedHistory.size > 0}
            <!-- Grouped History List -->
            <div class="space-y-6 mb-12">
                {#each [...groupedHistory.entries()] as [source, scrapes], i}
                    <div
                        class="glass-panel rounded-2xl overflow-hidden animate-slide-up"
                        style="animation-delay: {i * 100}ms"
                    >
                        <!-- Source Group Header -->
                        <div
                            class="p-5 sm:p-6 cursor-pointer hover:bg-white/5 transition-all duration-300 flex justify-between items-center group relative overflow-hidden"
                            role="button"
                            tabindex="0"
                            onclick={() => toggleSource(source)}
                            onkeydown={(e) =>
                                (e.key === "Enter" || e.key === " ") &&
                                toggleSource(source)}
                        >
                            {#if expandedSources.has(source)}
                                <div
                                    class="absolute left-0 top-0 bottom-0 w-1 bg-gradient-to-b from-primary-400 to-primary-600 shadow-[0_0_15px_rgba(124,58,237,0.8)]"
                                ></div>
                            {/if}

                            <div class="flex-1 min-w-0 pl-2">
                                <div class="flex items-center gap-3 mb-2">
                                    <h2
                                        class="text-2xl sm:text-3xl font-bold text-white font-outfit tracking-wide group-hover:text-primary-400 transition-colors"
                                    >
                                        {source}
                                    </h2>
                                    <span
                                        class="px-3 py-1 rounded-full bg-primary-500/20 text-primary-300 text-xs font-bold border border-primary-500/30"
                                    >
                                        {scrapes.length} Run{scrapes.length !==
                                        1
                                            ? "s"
                                            : ""}
                                    </span>
                                </div>
                                <div
                                    class="flex items-center gap-4 text-sm text-gray-400"
                                >
                                    <span class="flex items-center gap-1">
                                        <span class="text-primary-500">🗓️</span>
                                        Latest: {formatShortDate(
                                            scrapes[0].scrapeDate,
                                        )}
                                    </span>
                                </div>
                            </div>
                            <div
                                class="w-10 h-10 rounded-full bg-white/5 flex items-center justify-center text-primary-400 transition-transform duration-300 ml-4 group-hover:bg-primary-500/20"
                            >
                                <span
                                    class="transform transition-transform duration-300 {expandedSources.has(
                                        source,
                                    )
                                        ? 'rotate-180'
                                        : ''}">▼</span
                                >
                            </div>
                        </div>

                        <!-- Scrapes List -->
                        {#if expandedSources.has(source)}
                            <div
                                class="bg-black/20 p-4 sm:p-6 border-t border-white/5 animate-fade-in"
                            >
                                <div class="space-y-4">
                                    {#each scrapes as scrape, j}
                                        <div
                                            class="bg-white/5 rounded-xl border border-white/5 overflow-hidden hover:border-white/10 transition-colors duration-300"
                                        >
                                            <!-- Individual Scrape Header -->
                                            <div
                                                class="p-4 cursor-pointer hover:bg-white/5 transition-colors duration-200 flex justify-between items-center group"
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
                                                <div
                                                    class="flex items-center gap-4"
                                                >
                                                    <div
                                                        class="w-12 h-12 rounded-lg bg-black/40 flex flex-col items-center justify-center border border-white/10 shadow-inner"
                                                    >
                                                        <span
                                                            class="text-xs text-gray-400 font-bold"
                                                            >{new Date(
                                                                scrape.scrapeDate,
                                                            ).getDate()}</span
                                                        >
                                                        <span
                                                            class="text-[10px] text-primary-400 uppercase"
                                                            >{new Date(
                                                                scrape.scrapeDate,
                                                            ).toLocaleString(
                                                                "default",
                                                                {
                                                                    month: "short",
                                                                },
                                                            )}</span
                                                        >
                                                    </div>
                                                    <div>
                                                        <p
                                                            class="text-gray-300 text-sm font-medium mb-1"
                                                        >
                                                            {new Date(
                                                                scrape.scrapeDate,
                                                            ).toLocaleTimeString(
                                                                [],
                                                                {
                                                                    hour: "2-digit",
                                                                    minute: "2-digit",
                                                                },
                                                            )}
                                                        </p>
                                                        <div
                                                            class="flex items-center gap-2"
                                                        >
                                                            <span
                                                                class="text-white font-bold"
                                                                >{Object.keys(
                                                                    scrape.moviesScraped,
                                                                ).length}</span
                                                            >
                                                            <span
                                                                class="text-gray-400 text-sm"
                                                                >films
                                                                discovered</span
                                                            >
                                                        </div>
                                                    </div>
                                                </div>
                                                <div
                                                    class="text-gray-500 group-hover:text-primary-400 transition-colors"
                                                >
                                                    <span
                                                        class="text-xl transform transition-transform duration-300 inline-block {expandedScrapes.has(
                                                            scrape.id || '',
                                                        )
                                                            ? 'rotate-180'
                                                            : ''}">▼</span
                                                    >
                                                </div>
                                            </div>

                                            <!-- Movies List -->
                                            {#if expandedScrapes.has(scrape.id || "")}
                                                <div
                                                    class="p-4 bg-black/40 border-t border-white/5 animate-fade-in"
                                                >
                                                    <div
                                                        class="grid grid-cols-1 lg:grid-cols-2 gap-3"
                                                    >
                                                        {#each Object.entries(scrape.moviesScraped) as [title, imdbId]}
                                                            <div
                                                                class="group flex items-center justify-between p-3 rounded-lg bg-white/5 border border-white/5 hover:border-primary-500/30 hover:bg-primary-500/10 transition-all duration-300"
                                                            >
                                                                <div
                                                                    class="flex items-center gap-3 overflow-hidden"
                                                                >
                                                                    <div
                                                                        class="w-8 h-8 rounded-full bg-black/50 flex items-center justify-center shrink-0 shadow-inner"
                                                                    >
                                                                        <span
                                                                            class="text-sm"
                                                                            >🎬</span
                                                                        >
                                                                    </div>
                                                                    <span
                                                                        class="text-gray-200 text-sm font-medium truncate group-hover:text-white transition-colors"
                                                                        {title}
                                                                    >
                                                                        {title}
                                                                    </span>
                                                                </div>
                                                                <button
                                                                    onclick={() =>
                                                                        viewFilm(
                                                                            imdbId,
                                                                        )}
                                                                    disabled={isLoadingFilm}
                                                                    class="shrink-0 ml-3 bg-white/10 hover:bg-primary-500 text-white hover:text-white disabled:opacity-50 text-xs font-semibold py-1.5 px-3 rounded-md transition-all duration-300 flex items-center gap-1 border border-white/10 hover:border-primary-400 hover:shadow-[0_0_10px_rgba(124,58,237,0.5)]"
                                                                >
                                                                    {#if isLoadingFilm}
                                                                        <span
                                                                            class="w-3 h-3 border-2 border-white/30 border-t-white rounded-full animate-spin"
                                                                        ></span>
                                                                    {:else}
                                                                        <span
                                                                            >Details</span
                                                                        ><span
                                                                            class="opacity-0 group-hover:opacity-100 -translate-x-2 group-hover:translate-x-0 transition-all duration-300"
                                                                            >→</span
                                                                        >
                                                                    {/if}
                                                                </button>
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
        {#if history.length > 0}
            <footer class="text-center py-8 border-t border-white/10 mt-8">
                <div
                    class="inline-flex items-center gap-3 px-6 py-2 rounded-full glass-panel border border-white/5"
                >
                    <span
                        class="w-2 h-2 rounded-full bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.6)] animate-pulse"
                    ></span>
                    <p class="text-sm font-medium text-gray-300">
                        Total Records: <span class="text-white font-bold"
                            >{history.length}</span
                        >
                        <span class="opacity-50 mx-2">|</span>
                        Sources:
                        <span class="text-white font-bold"
                            >{groupedHistory.size}</span
                        >
                    </p>
                </div>
            </footer>
        {/if}
    </div>
</div>
