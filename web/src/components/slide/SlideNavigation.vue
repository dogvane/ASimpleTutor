<template>
  <div class="slide-navigation">
    <button
      class="nav-btn prev"
      @click="$emit('prev')"
      :disabled="!hasPrev"
    >
      &lt; Prev
    </button>
    <span class="slide-counter">
      {{ currentIndex + 1 }} / {{ total }}
    </span>
    <button
      class="nav-btn next"
      @click="$emit('next')"
      :disabled="!hasNext"
    >
      Next &gt;
    </button>
    <button
      v-if="showNextChapter"
      class="nav-btn next-chapter"
      @click="$emit('next-chapter')"
    >
      下一章 &raquo;
    </button>
  </div>
</template>

<script setup>
defineProps({
  currentIndex: {
    type: Number,
    default: 0,
  },
  total: {
    type: Number,
    default: 0,
  },
  hasNext: {
    type: Boolean,
    default: false,
  },
  hasPrev: {
    type: Boolean,
    default: false,
  },
  showNextChapter: {
    type: Boolean,
    default: false,
  },
})

defineEmits(['prev', 'next', 'next-chapter'])
</script>

<style scoped>
.slide-navigation {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 15px;
}

.nav-btn {
  background: #f8f9fa;
  border: 1px solid #dee2e6;
  border-radius: 6px;
  padding: 10px 20px;
  font-size: 16px;
  cursor: pointer;
  transition: all 0.2s;
}

.nav-btn:hover:not(:disabled) {
  background: #e9ecef;
  border-color: #adb5bd;
}

.nav-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.nav-btn.prev {
  order: 1;
}

.nav-btn.next {
  order: 3;
}

.nav-btn.next-chapter {
  order: 4;
  background: #10b981;
  border-color: #10b981;
  color: white;
}

.nav-btn.next-chapter:hover:not(:disabled) {
  background: #059669;
  border-color: #059669;
}

.slide-counter {
  order: 2;
  font-size: 16px;
  font-weight: 500;
  color: #6c757d;
}
</style>
