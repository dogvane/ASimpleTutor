<script setup>
import { computed, ref, watch } from 'vue'

const props = defineProps({
  slides: {
    type: Array,
    default: () => []
  },
  currentIndex: {
    type: Number,
    default: 0
  },
  title: {
    type: String,
    default: ''
  },
  audioAvailable: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['update:currentIndex', 'slideChange', 'openExercises'])

const audioPlaying = ref(false)
const hoverSource = ref(null)

const currentSlide = computed(() => {
  if (props.slides.length === 0) return null
  return props.slides[Math.max(0, Math.min(props.currentIndex, props.slides.length - 1))]
})

const hasNext = computed(() => props.currentIndex < props.slides.length - 1)
const hasPrev = computed(() => props.currentIndex > 0)

const nextSlide = () => {
  if (hasNext.value) {
    const newIndex = props.currentIndex + 1
    emit('update:currentIndex', newIndex)
    emit('slideChange', newIndex)
  } else {
    // 当到达当前知识点的最后一张幻灯片时，触发知识点切换
    emit('slideChange', 'next_knowledge_point')
  }
}

const prevSlide = () => {
  if (hasPrev.value) {
    const newIndex = props.currentIndex - 1
    emit('update:currentIndex', newIndex)
    emit('slideChange', newIndex)
  }
}

const toggleAudio = () => {
  audioPlaying.value = !audioPlaying.value
}

const handleSourceHover = (source) => {
  hoverSource.value = source
}

const handleSourceLeave = () => {
  hoverSource.value = null
}

const handleKeydown = (event) => {
  if (event.key === 'ArrowRight' || event.key === 'ArrowDown' || event.key === ' ') {
    event.preventDefault()
    nextSlide()
  } else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
    event.preventDefault()
    prevSlide()
  }
}

const handleSlideClick = (event) => {
  // 如果是习题幻灯片，点击时打开习题抽屉
  if (currentSlide.value?.type === 'quiz') {
    emit('openExercises')
    return
  }
  
  const rect = event.currentTarget.getBoundingClientRect()
  const clickX = event.clientX - rect.left
  if (clickX < rect.width * 0.3) {
    prevSlide()
  } else if (clickX > rect.width * 0.7) {
    nextSlide()
  }
}

const handleKpLinkClick = (kpId) => {
  // 处理知识点链接点击
  emit('slideChange', kpId)
}

// 监听键盘事件
const setupKeyboardListener = () => {
  window.addEventListener('keydown', handleKeydown)
  return () => {
    window.removeEventListener('keydown', handleKeydown)
  }
}

setupKeyboardListener()

// 监听幻灯片变化
watch(() => props.currentIndex, (newIndex) => {
  // 停止当前音频
  audioPlaying.value = false
})
</script>

<template>
  <div class="slide-viewer">
    <div class="slide-header">
      <h2>{{ title }}</h2>
      <button 
        v-if="audioAvailable" 
        class="audio-toggle" 
        @click="toggleAudio"
        :class="{ playing: audioPlaying }"
      >
        {{ audioPlaying ? '🔊' : '🔈' }}
      </button>
    </div>

    <div class="slide-container">
      <div 
        v-if="currentSlide" 
        class="slide-card" 
        @click="handleSlideClick"
      >
        <!-- 幻灯片内容 -->
        <div class="slide-content">
          <!-- 显示幻灯片内容 -->
          <div v-if="currentSlide.content" v-html="currentSlide.content"></div>
          
          <!-- 为有原文来源的文本添加悬停事件 -->
          <div v-if="currentSlide.sources && currentSlide.sources.length > 0" class="sources-container">
            <h3>原文来源</h3>
            <div 
              v-for="(source, index) in currentSlide.sources" 
              :key="index"
              class="source-item"
            >
              <div class="source-file">{{ source.fileName }}</div>
              <div class="source-content">{{ source.content }}</div>
            </div>
          </div>
        </div>

        <!-- 原文悬停提示 -->
        <div 
          v-if="hoverSource" 
          class="source-tooltip"
        >
          <div class="tooltip-header">原文来源</div>
          <div class="tooltip-content">{{ hoverSource.content }}</div>
          <div class="tooltip-footer">{{ hoverSource.fileName }}:{{ hoverSource.lineStart }}</div>
        </div>
      </div>

      <div v-else class="empty-slide">
        <p>暂无幻灯片内容</p>
      </div>
    </div>

    <div class="slide-navigation">
      <button 
        class="nav-btn prev"
        @click="prevSlide"
        :disabled="!hasPrev"
      >
        &lt; Prev
      </button>
      <span class="slide-counter">
        {{ currentIndex + 1 }} / {{ slides.length }}
      </span>
      <button 
        class="nav-btn next"
        @click="nextSlide"
        :disabled="!hasNext"
      >
        Next &gt;
      </button>
    </div>
  </div>
</template>

<style scoped>
.slide-viewer {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 20px;
}

.slide-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.slide-header h2 {
  margin: 0;
  font-size: 24px;
  font-weight: 600;
}

.audio-toggle {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  padding: 0;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: background-color 0.2s;
}

.audio-toggle:hover {
  background-color: rgba(0, 0, 0, 0.05);
}

.audio-toggle.playing {
  background-color: rgba(0, 123, 255, 0.1);
}

.slide-container {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 30px;
  position: relative;
}

.slide-card {
  width: 100%;
  max-width: 800px;
  min-height: 400px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
  padding: 40px;
  cursor: pointer;
  transition: transform 0.3s, box-shadow 0.3s;
  position: relative;
  overflow: hidden;
}

.slide-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 30px rgba(0, 0, 0, 0.15);
}

.slide-content {
  font-size: 18px;
  line-height: 1.6;
}

.slide-content h1, .slide-content h2, .slide-content h3 {
  margin-top: 0;
  margin-bottom: 20px;
}

.slide-content p {
  margin-bottom: 20px;
}

.slide-content a {
  color: #007bff;
  text-decoration: underline;
  cursor: pointer;
}

.slide-content a:hover {
  color: #0056b3;
}

.source-hover {
  border-bottom: 1px dashed #007bff;
  cursor: help;
  position: relative;
}

.sources-container {
  margin-top: 30px;
  padding-top: 20px;
  border-top: 1px solid #e9ecef;
}

.source-item {
  margin-bottom: 20px;
  padding: 15px;
  background-color: #f8f9fa;
  border-radius: 8px;
}

.source-file {
  font-weight: 600;
  margin-bottom: 10px;
  color: #495057;
}

.source-content {
  font-size: 14px;
  line-height: 1.5;
  color: #6c757d;
}

.source-tooltip {
  position: absolute;
  background: rgba(0, 0, 0, 0.9);
  color: white;
  padding: 15px;
  border-radius: 8px;
  max-width: 300px;
  z-index: 1000;
  font-size: 14px;
  line-height: 1.4;
}

.tooltip-header {
  font-weight: 600;
  margin-bottom: 8px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.2);
  padding-bottom: 5px;
}

.tooltip-content {
  margin-bottom: 8px;
}

.tooltip-footer {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.7);
}

.empty-slide {
  width: 100%;
  max-width: 800px;
  min-height: 400px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f8f9fa;
  border-radius: 12px;
  border: 2px dashed #dee2e6;
}

.slide-navigation {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 20px;
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

.slide-counter {
  order: 2;
  font-size: 16px;
  font-weight: 500;
  color: #6c757d;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .slide-card {
    padding: 20px;
    min-height: 300px;
  }

  .slide-content {
    font-size: 16px;
  }

  .slide-header h2 {
    font-size: 20px;
  }
}
</style>