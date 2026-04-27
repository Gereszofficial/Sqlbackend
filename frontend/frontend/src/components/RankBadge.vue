<template>
  <div class="rank-wrap" :class="wrapClass" :style="wrapStyle" :title="label">
    <div class="rank-hex" :class="hexClass">
      <svg class="rank-star" viewBox="0 0 24 24" aria-hidden="true">
        <path
          d="M12 2.6l2.74 5.55 6.12.89-4.43 4.32 1.05 6.1L12 16.98 6.52 19.46l1.05-6.1L3.14 9.04l6.12-.89L12 2.6z"
          fill="currentColor"
        />
      </svg>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { RankBadge as RankBadgeType } from '@/types/api'
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    badge: RankBadgeType
    size?: number
  }>(),
  { size: 28 }
)

const label = computed(() => props.badge?.label || '')
const wrapStyle = computed(() => ({ width: `${props.size}px`, height: `${props.size}px` }))

const wrapClass = computed(() => `rank-${props.badge?.key || 'rookie'}`)
const hexClass = computed(() => `rank-${props.badge?.key || 'rookie'}`)
</script>

<style scoped>
.rank-wrap {
  display: inline-grid;
  place-items: center;
  border-radius: 999px;
}

.rank-hex {
  width: 100%;
  height: 100%;
  display: grid;
  place-items: center;
  clip-path: polygon(25% 0%, 75% 0%, 100% 50%, 75% 100%, 25% 100%, 0% 50%);
  border: 1px solid rgba(255, 255, 255, 0.14);
  box-shadow:
    0 0 0 1px rgba(0, 0, 0, 0.6) inset,
    0 8px 18px rgba(0, 0, 0, 0.35);
  position: relative;
  overflow: hidden;
}

.rank-star {
  width: 56%;
  height: 56%;
  color: rgba(255, 255, 255, 0.92);
  filter: drop-shadow(0 2px 2px rgba(0, 0, 0, 0.55));
}

/* Base gradients */
.rank-rookie {
  background: radial-gradient(70% 70% at 30% 25%, rgba(52, 211, 153, 0.9), rgba(16, 185, 129, 0.25) 55%, rgba(2, 6, 23, 0.2));
}
.rank-bronze {
  background: radial-gradient(70% 70% at 30% 25%, rgba(251, 146, 60, 0.95), rgba(154, 52, 18, 0.35) 55%, rgba(2, 6, 23, 0.2));
}
.rank-silver {
  background: radial-gradient(70% 70% at 30% 25%, rgba(148, 163, 184, 0.95), rgba(71, 85, 105, 0.35) 55%, rgba(2, 6, 23, 0.2));
}
.rank-gold {
  background: radial-gradient(70% 70% at 30% 25%, rgba(251, 191, 36, 0.95), rgba(146, 64, 14, 0.35) 55%, rgba(2, 6, 23, 0.2));
}
.rank-platinum {
  background: radial-gradient(70% 70% at 30% 25%, rgba(56, 189, 248, 0.95), rgba(14, 116, 144, 0.35) 55%, rgba(2, 6, 23, 0.2));
}

/* Legends: animated "tüzes"/mozgó */
.rank-diamond_legends {
  background: radial-gradient(80% 80% at 30% 25%, rgba(167, 139, 250, 0.9), rgba(59, 130, 246, 0.25) 55%, rgba(2, 6, 23, 0.2));
}

.rank-hex.rank-diamond_legends::before {
  content: '';
  position: absolute;
  inset: -40%;
  background: conic-gradient(
    from 180deg,
    rgba(255, 154, 60, 0.0),
    rgba(255, 154, 60, 0.65),
    rgba(255, 60, 0, 0.45),
    rgba(168, 85, 247, 0.55),
    rgba(59, 130, 246, 0.55),
    rgba(255, 154, 60, 0.65),
    rgba(255, 154, 60, 0.0)
  );
  filter: blur(10px);
  opacity: 0.9;
  animation: spinFire 1.6s linear infinite;
}

.rank-hex.rank-diamond_legends::after {
  content: '';
  position: absolute;
  inset: 0;
  background: radial-gradient(60% 55% at 50% 70%, rgba(255, 154, 60, 0.55), rgba(255, 60, 0, 0) 62%),
    radial-gradient(45% 45% at 30% 30%, rgba(168, 85, 247, 0.55), rgba(168, 85, 247, 0) 60%),
    radial-gradient(45% 45% at 75% 35%, rgba(59, 130, 246, 0.45), rgba(59, 130, 246, 0) 60%);
  mix-blend-mode: screen;
  opacity: 0.9;
  animation: pulseGlow 1.25s ease-in-out infinite;
}

@keyframes spinFire {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

@keyframes pulseGlow {
  0%,
  100% {
    transform: scale(1);
    opacity: 0.75;
  }
  50% {
    transform: scale(1.05);
    opacity: 1;
  }
}

/* Keep star above effects */
.rank-hex > .rank-star {
  position: relative;
  z-index: 2;
}
</style>
