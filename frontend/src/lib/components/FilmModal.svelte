<script lang="ts">
    import type { Film } from "$lib/types/film";
    import { enhance } from "$app/forms";

    interface Props {
        film: Film | null;
        isOpen: boolean;
        onClose: () => void;
    }

    let { film, isOpen, onClose }: Props = $props();
    let isSubmitting = $state(false);
    let showSuccess = $state(false);
    let errorMessage = $state<string | null>(null);

    function handleBackdropClick(e: MouseEvent) {
        if (e.target === e.currentTarget) {
            onClose();
        }
    }

    function handleKeydown(e: KeyboardEvent) {
        if (e.key === "Escape") {
            onClose();
        }
    }
</script>

{#if isOpen && film}
    <div
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md transition-opacity animate-fade-in"
        onclick={handleBackdropClick}
        onkeydown={handleKeydown}
        role="dialog"
        tabindex="-1"
        aria-modal="true"
        aria-labelledby="modal-title"
    >
        <div
            class="glass-panel rounded-2xl shadow-2xl max-w-2xl w-full max-h-[90vh] overflow-y-auto animate-slide-up relative overflow-hidden"
        >
            <!-- Background Accent -->
            <div
                class="absolute top-0 right-0 w-64 h-64 bg-primary-500/20 rounded-full blur-3xl -z-10 pointer-events-none"
            ></div>

            <!-- Header -->
            <div
                class="sticky top-0 bg-black/40 backdrop-blur-md border-b border-white/10 p-6 flex justify-between items-start z-10"
            >
                <h2
                    id="modal-title"
                    class="text-3xl font-bold text-white pr-8 font-outfit filter drop-shadow-[0_0_8px_rgba(255,255,255,0.2)]"
                >
                    {film.title}
                </h2>
                <button
                    onclick={onClose}
                    class="text-gray-400 hover:text-white hover:bg-white/10 rounded-full transition-all p-2 -mr-2 -mt-2"
                    aria-label="Close modal"
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
            </div>

            <!-- Content -->
            <div class="p-6 relative z-0">
                <!-- Poster Image -->
                {#if film.posterUrl && film.posterUrl !== "N/A"}
                    <div class="mb-8 flex justify-center">
                        <div
                            class="relative group rounded-xl overflow-hidden shadow-2xl border border-white/10"
                        >
                            <div
                                class="absolute inset-0 bg-primary-500/20 opacity-0 group-hover:opacity-100 transition-opacity duration-300 z-10"
                            ></div>
                            <img
                                src={film.posterUrl}
                                alt={film.title}
                                class="max-h-[400px] object-cover"
                            />
                        </div>
                    </div>
                {:else}
                    <div
                        class="mb-8 flex justify-center items-center bg-black/40 border border-white/10 rounded-xl shadow-inner h-[400px] w-64 mx-auto"
                    >
                        <span class="text-8xl opacity-50">🎬</span>
                    </div>
                {/if}

                <!-- Film Details -->
                <div
                    class="grid grid-cols-1 md:grid-cols-2 gap-6 bg-white/5 p-6 rounded-xl border border-white/5"
                >
                    {#if film.year}
                        <div class="flex items-start gap-4">
                            <div class="p-2 bg-primary-500/20 rounded-lg">
                                <span class="text-xl">📅</span>
                            </div>
                            <div>
                                <p
                                    class="text-xs text-primary-300 font-semibold uppercase tracking-wider mb-1"
                                >
                                    Year
                                </p>
                                <p class="text-lg text-white font-medium">
                                    {film.year}
                                </p>
                            </div>
                        </div>
                    {/if}

                    {#if film.country}
                        <div class="flex items-start gap-4">
                            <div class="p-2 bg-primary-500/20 rounded-lg">
                                <span class="text-xl">🌍</span>
                            </div>
                            <div>
                                <p
                                    class="text-xs text-primary-300 font-semibold uppercase tracking-wider mb-1"
                                >
                                    Country
                                </p>
                                <p class="text-lg text-white font-medium">
                                    {film.country}
                                </p>
                            </div>
                        </div>
                    {/if}

                    <div class="flex items-start gap-4 md:col-span-2">
                        <div class="p-2 bg-primary-500/20 rounded-lg">
                            <span class="text-xl">🎞️</span>
                        </div>
                        <div>
                            <p
                                class="text-xs text-primary-300 font-semibold uppercase tracking-wider mb-1"
                            >
                                TMDB ID
                            </p>
                            <p class="text-lg font-mono">
                                <a
                                    href="https://www.themoviedb.org/movie/{film.tmdbId}"
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    class="text-primary-400 hover:text-primary-300 transition-colors border-b border-primary-500/30 hover:border-primary-400 pb-0.5"
                                >
                                    {film.tmdbId}
                                </a>
                            </p>
                        </div>
                    </div>
                </div>

                <!-- Radarr Link -->
                {#if film.isInRadarr}
                    <div class="mt-8">
                        <div
                            class="flex items-center justify-center gap-3 mb-4 text-green-400 font-medium"
                        >
                            <span
                                class="w-2 h-2 rounded-full bg-green-400 drop-shadow-[0_0_5px_rgba(74,222,128,0.8)]"
                            ></span>
                            Already in your collection
                        </div>
                        <a
                            href="https://radarr.caddy.mazilious.org/movie/{film.tmdbId}"
                            target="_blank"
                            rel="noopener noreferrer"
                            class="block w-full btn-secondary text-center font-bold"
                        >
                            Open in Radarr ↗
                        </a>
                    </div>
                {/if}
                {#if !film.isInRadarr}
                    <div class="mt-8">
                        {#if showSuccess}
                            <div
                                class="bg-green-500/10 border border-green-500/30 rounded-xl p-4 mb-6 animate-fade-in"
                            >
                                <div class="flex items-center gap-4">
                                    <div
                                        class="p-2 bg-green-500/20 rounded-full"
                                    >
                                        <span
                                            class="text-2xl drop-shadow-[0_0_8px_rgba(34,197,94,0.6)]"
                                            >✅</span
                                        >
                                    </div>
                                    <div>
                                        <p
                                            class="text-green-400 font-semibold text-lg"
                                        >
                                            Successfully added to Radarr!
                                        </p>
                                        <p class="text-green-500/80 text-sm">
                                            The film is now downloading
                                        </p>
                                    </div>
                                </div>
                            </div>
                        {/if}
                        {#if errorMessage}
                            <div
                                class="bg-red-500/10 border border-red-500/30 rounded-xl p-4 mb-6 animate-fade-in"
                            >
                                <div class="flex items-center gap-4">
                                    <div class="p-2 bg-red-500/20 rounded-full">
                                        <span
                                            class="text-2xl drop-shadow-[0_0_8px_rgba(239,68,68,0.6)]"
                                            >❌</span
                                        >
                                    </div>
                                    <div class="flex-1">
                                        <p
                                            class="text-red-400 font-semibold text-lg"
                                        >
                                            Failed to add to Radarr
                                        </p>
                                        <p class="text-red-500/80 text-sm">
                                            {errorMessage}
                                        </p>
                                    </div>
                                    <button
                                        onclick={() => (errorMessage = null)}
                                        class="text-gray-400 hover:text-white hover:bg-white/10 p-2 rounded-full transition-colors"
                                        aria-label="Dismiss error"
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
                                </div>
                            </div>
                        {/if}
                        <form
                            method="POST"
                            action="/films?/addToRadarr"
                            use:enhance={() => {
                                isSubmitting = true;
                                showSuccess = false;
                                errorMessage = null;
                                return async ({ result }) => {
                                    isSubmitting = false;
                                    if (result.type === "success") {
                                        showSuccess = true;
                                        errorMessage = null;
                                        if (film) {
                                            film.isInRadarr = true;
                                        }
                                        setTimeout(() => {
                                            showSuccess = false;
                                        }, 3000);
                                    } else if (result.type === "failure") {
                                        errorMessage =
                                            (result.data as { error?: string })
                                                ?.error ||
                                            "An unexpected error occurred";
                                    }
                                };
                            }}
                        >
                            <input
                                type="hidden"
                                name="tmdbId"
                                value={film.tmdbId}
                            />
                            <button
                                type="submit"
                                disabled={isSubmitting}
                                class="w-full btn-primary text-lg flex items-center justify-center gap-3 disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                                {#if isSubmitting}
                                    <svg
                                        class="animate-spin h-5 w-5 text-white"
                                        xmlns="http://www.w3.org/2000/svg"
                                        fill="none"
                                        viewBox="0 0 24 24"
                                    >
                                        <circle
                                            class="opacity-25"
                                            cx="12"
                                            cy="12"
                                            r="10"
                                            stroke="currentColor"
                                            stroke-width="4"
                                        ></circle>
                                        <path
                                            class="opacity-75"
                                            fill="currentColor"
                                            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                                        ></path>
                                    </svg>
                                    <span>Processing Request...</span>
                                {:else}
                                    <span>Add to Radarr</span>
                                    <span
                                        class="text-primary-300 group-hover:text-white transition-colors text-xl"
                                        >→</span
                                    >
                                {/if}
                            </button>
                        </form>
                    </div>
                {/if}
            </div>

            <!-- Footer -->
            <div class="p-6 bg-black/40 border-t border-white/10 z-10 relative">
                <button
                    onclick={onClose}
                    class="w-full bg-white/5 hover:bg-white/10 border border-white/10 text-gray-300 font-semibold py-3 px-6 rounded-xl transition-all duration-300"
                >
                    Close Dialog
                </button>
            </div>
        </div>
    </div>
{/if}
