<script lang="ts">
    export let errorMessage: string;
    export let hint: string = "Make sure the backend API is running.";
</script>

<div
    class="glass-panel border-l-4 border-l-red-500 p-6 mb-8 rounded-xl animate-slide-up"
>
    <div class="flex items-start gap-4">
        <div class="p-3 bg-red-500/20 rounded-full flex-shrink-0">
            <span class="text-2xl drop-shadow-[0_0_8px_rgba(239,68,68,0.6)]"
                >⚠️</span
            >
        </div>
        <div>
            <h3
                class="text-red-400 font-bold text-lg mb-1 filter drop-shadow-[0_0_5px_rgba(248,113,113,0.3)]"
            >
                {errorMessage}
            </h3>
            {#if hint}
                <p class="text-gray-400 text-sm">{hint}</p>
            {/if}
        </div>
    </div>
</div>
