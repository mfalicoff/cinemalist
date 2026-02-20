<script lang="ts">
    export let icon: string;
    export let title: string;
    export let description: string;
</script>

<div
    class="glass-panel p-12 text-center rounded-2xl animate-slide-up border border-white/5"
>
    <div
        class="text-6xl mb-4 filter drop-shadow-[0_0_15px_rgba(255,255,255,0.2)]"
    >
        {icon}
    </div>
    <h3 class="text-2xl font-bold text-white mb-2">{title}</h3>
    <p class="text-gray-400">{description}</p>
</div>
