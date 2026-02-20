<script lang="ts">
    export let value: string = "";
    export let placeholder: string = "Search...";
    export let onClear: () => void = () => {
        value = "";
    };
</script>

<div class="relative max-w-2xl mx-auto mb-12 animate-slide-up group">
    <!-- Glow effect behind search bar -->
    <div
        class="absolute -inset-1 bg-gradient-to-r from-primary-600 to-primary-400 rounded-2xl blur opacity-20 group-hover:opacity-40 transition duration-500"
    ></div>

    <div class="relative flex items-center">
        <!-- Search Icon -->
        <div
            class="absolute left-4 z-10 text-gray-400 group-hover:text-primary-400 transition-colors"
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
                    d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                />
            </svg>
        </div>

        <!-- Input Field -->
        <input
            type="text"
            bind:value
            {placeholder}
            class="w-full glass-input py-4 pl-14 pr-12 rounded-2xl text-lg font-outfit tracking-wide"
        />

        <!-- Clear Search Button -->
        {#if value}
            <button
                onclick={onClear}
                class="absolute right-4 z-10 text-gray-400 hover:text-white hover:bg-white/10 p-1.5 rounded-full transition-all"
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
