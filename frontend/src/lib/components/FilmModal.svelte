<script lang="ts">
    import type { Film } from "$lib/types/film";

    interface Props {
        film: Film | null;
        isOpen: boolean;
        onClose: () => void;
    }

    let { film, isOpen, onClose }: Props = $props();

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
                    {#if film.director}
                        <div class="flex items-start gap-3">
                            <span class="text-2xl">👤</span>
                            <div>
                                <p class="text-sm text-gray-500 font-medium">
                                    Director
                                </p>
                                <p class="text-lg text-gray-900">
                                    {film.director}
                                </p>
                            </div>
                        </div>
                    {/if}

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
                                {film.imbdId}
                            </p>
                        </div>
                    </div>
                </div>

                <!-- IMDb Link -->
                <div class="mt-8">
                    {console.log(film, film.imbdId)}
                    <a
                        href="https://www.imdb.com/title/{film.imbdId}"
                        target="_blank"
                        rel="noopener noreferrer"
                        class="block w-full bg-primary-500 hover:bg-primary-600 text-white font-bold py-3 px-6 rounded-lg transition-colors duration-200 text-center"
                    >
                        View on IMDb →
                    </a>
                </div>
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
