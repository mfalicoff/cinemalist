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
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black bg-opacity-50"
        onclick={handleBackdropClick}
        onkeydown={handleKeydown}
        role="dialog"
        tabindex="-1"
        aria-modal="true"
        aria-labelledby="modal-title"
    >
        <div
            class="bg-white rounded-xl shadow-2xl max-w-2xl w-full max-h-[90vh] overflow-y-auto"
        >
            <!-- Header -->
            <div
                class="sticky top-0 bg-white border-b border-gray-200 p-6 flex justify-between items-start"
            >
                <h2
                    id="modal-title"
                    class="text-3xl font-bold text-gray-900 pr-8"
                >
                    {film.title}
                </h2>
                <button
                    onclick={onClose}
                    class="text-gray-400 hover:text-gray-600 transition-colors p-2 -mr-2 -mt-2"
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
            <div class="p-6">
                <!-- Poster Image -->
                {#if film.posterUrl && film.posterUrl !== "N/A"}
                    <div class="mb-6 flex justify-center">
                        <img
                            src={film.posterUrl}
                            alt={film.title}
                            class="rounded-lg shadow-lg max-h-96 object-cover"
                        />
                    </div>
                {:else}
                    <div
                        class="mb-6 flex justify-center items-center bg-gradient-to-br from-primary-500 to-primary-700 rounded-lg shadow-lg h-96 w-64 mx-auto"
                    >
                        <span class="text-8xl">🎬</span>
                    </div>
                {/if}

                <!-- Film Details -->
                <div class="space-y-4">
                    {#if film.year}
                        <div class="flex items-start gap-3">
                            <span class="text-2xl">📅</span>
                            <div>
                                <p class="text-sm text-gray-500 font-medium">
                                    Year
                                </p>
                                <p class="text-lg text-gray-900">{film.year}</p>
                            </div>
                        </div>
                    {/if}

                    {#if film.country}
                        <div class="flex items-start gap-3">
                            <span class="text-2xl">🌍</span>
                            <div>
                                <p class="text-sm text-gray-500 font-medium">
                                    Country
                                </p>
                                <p class="text-lg text-gray-900">
                                    {film.country}
                                </p>
                            </div>
                        </div>
                    {/if}

                    <div class="flex items-start gap-3">
                        <span class="text-2xl">🎞️</span>
                        <div>
                            <p class="text-sm text-gray-500 font-medium">
                                IMDb ID
                            </p>
                            <p class="text-lg text-gray-900 font-mono">
                                <a
                                    href="https://www.imdb.com/title/{film.imdbId}"
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    class="text-blue-400"
                                >
                                    {film.imdbId}
                                </a>
                            </p>
                        </div>
                    </div>
                </div>

                <!-- Radarr Link -->
                {#if film.isInRadarr}
                    <div class="mt-8">
                        <a
                            href="https://radarr.caddy.mazilious.org/movie/{film.tmdbId}"
                            target="_blank"
                            rel="noopener noreferrer"
                            class="block w-full bg-primary-500 hover:bg-primary-600 text-white font-bold py-3 px-6 rounded-lg transition-colors duration-200 text-center"
                        >
                            View in Radarr →
                        </a>
                    </div>
                {/if}
                {#if !film.isInRadarr}
                    <div class="mt-8">
                        {#if showSuccess}
                            <div
                                class="bg-green-50 border-2 border-green-500 rounded-lg p-4 mb-4 animate-fade-in"
                            >
                                <div class="flex items-center gap-3">
                                    <span class="text-3xl">✅</span>
                                    <div>
                                        <p
                                            class="text-green-800 font-semibold text-lg"
                                        >
                                            Successfully added to Radarr!
                                        </p>
                                        <p class="text-green-600 text-sm">
                                            The film is now in your collection
                                        </p>
                                    </div>
                                </div>
                            </div>
                        {/if}
                        {#if errorMessage}
                            <div
                                class="bg-red-50 border-2 border-red-500 rounded-lg p-4 mb-4 animate-fade-in"
                            >
                                <div class="flex items-center gap-3">
                                    <span class="text-3xl">❌</span>
                                    <div class="flex-1">
                                        <p
                                            class="text-red-800 font-semibold text-lg"
                                        >
                                            Failed to add to Radarr
                                        </p>
                                        <p class="text-red-600 text-sm">
                                            {errorMessage}
                                        </p>
                                    </div>
                                    <button
                                        onclick={() => (errorMessage = null)}
                                        class="text-red-400 hover:text-red-600"
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
                                class="w-full bg-primary-500 hover:bg-primary-600 disabled:bg-gray-400 disabled:cursor-not-allowed text-white font-bold py-3 px-6 rounded-lg transition-colors duration-200 flex items-center justify-center gap-2"
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
                                    <span>Adding to Radarr...</span>
                                {:else}
                                    <span>Add to Radarr →</span>
                                {/if}
                            </button>
                        </form>
                    </div>
                {/if}
            </div>

            <!-- Footer -->
            <div class="border-t border-gray-200 p-6 bg-gray-50">
                <button
                    onclick={onClose}
                    class="w-full bg-gray-200 hover:bg-gray-300 text-gray-800 font-semibold py-3 px-6 rounded-lg transition-colors duration-200"
                >
                    Close
                </button>
            </div>
        </div>
    </div>
{/if}
