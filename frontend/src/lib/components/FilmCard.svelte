<script lang="ts">
    import type { Film } from "$lib/types/film";

    export let film: Film;
    export let index: number = 0;
    export let onClick: (film: Film) => void;
</script>

<button
    class="text-left group relative glass-panel-hover rounded-2xl overflow-hidden animate-slide-up w-full focus:outline-none focus:ring-2 focus:ring-primary-500"
    style="animation-delay: {index * 50}ms"
    onclick={() => onClick(film)}
    onkeydown={(e) => e.key === "Enter" && onClick(film)}
>
    <!-- Poster Container with Aspect Ratio Enforcement -->
    <div class="relative w-full aspect-[2/3] overflow-hidden bg-black/40">
        {#if film.posterUrl && film.posterUrl !== "N/A"}
            <img
                src={film.posterUrl}
                alt={film.title}
                loading="lazy"
                class="w-full h-full object-cover transition-transform duration-700 group-hover:scale-110"
            />
        {:else}
            <!-- Placeholder for missing poster -->
            <div class="absolute inset-0 flex items-center justify-center">
                <span
                    class="text-6xl opacity-30 group-hover:opacity-50 transition-opacity drop-shadow-xl"
                    >🎬</span
                >
            </div>
        {/if}

        <!-- Gradient Overlay for Text Readability -->
        <div
            class="absolute inset-0 bg-gradient-to-t from-gray-950/90 via-gray-950/20 to-transparent opacity-80 group-hover:opacity-100 transition-opacity duration-300"
        ></div>

        <!-- Hover "View Details" Overlay -->
        <div
            class="absolute inset-0 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity duration-300 bg-black/40 backdrop-blur-sm z-10"
        >
            <span
                class="btn-primary py-2 px-4 shadow-[0_0_15px_rgba(124,58,237,0.5)]"
                >View Details</span
            >
        </div>

        <!-- In Radarr Badge -->
        {#if film.isInRadarr}
            <div class="absolute top-3 right-3 z-20">
                <div
                    class="bg-black/60 backdrop-blur-md rounded-full px-3 py-1 flex items-center gap-2 border border-green-500/30 shadow-[0_0_10px_rgba(34,197,94,0.3)]"
                >
                    <div
                        class="w-2 h-2 rounded-full bg-green-400 drop-shadow-[0_0_5px_rgba(74,222,128,0.8)]"
                    ></div>
                    <span
                        class="text-xs font-semibold text-green-400 tracking-wider"
                        >In Radarr</span
                    >
                </div>
            </div>
        {/if}

        <!-- Year Badge -->
        {#if film.year}
            <div class="absolute top-3 left-3 z-20">
                <div
                    class="bg-black/60 backdrop-blur-md rounded-lg px-2 py-1 border border-white/10"
                >
                    <span class="text-xs font-medium text-white/90"
                        >{film.year}</span
                    >
                </div>
            </div>
        {/if}
    </div>

    <!-- Film Info Section -->
    <div
        class="p-4 relative z-20 h-[100px] flex flex-col justify-between bg-black/20 border-t border-white/5"
    >
        <h3
            class="font-bold text-lg text-white font-outfit line-clamp-2 leading-tight group-hover:text-primary-300 transition-colors"
        >
            {film.title}
        </h3>

        {#if film.country}
            <p class="text-sm text-gray-400 flex items-center gap-1.5 mt-2">
                <span class="text-xs opacity-70">🌍</span>
                {film.country}
            </p>
        {/if}
    </div>
</button>
