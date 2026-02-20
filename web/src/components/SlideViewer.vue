<script setup>
import { computed, ref, watch } from 'vue'
import { marked } from 'marked'
import QuizOptions from './slide/QuizOptions.vue'
import QuizFeedback from './slide/QuizFeedback.vue'
import SlideNavigation from './slide/SlideNavigation.vue'

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
  },
  quizAnswers: {
    type: Object,
    default: () => {}
  },
  quizFeedback: {
    type: Object,
    default: () => {}
  },
  hasNextChapter: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['update:currentIndex', 'slideChange', 'openExercises', 'updateQuizAnswer', 'submitQuizAnswer', 'nextChapter'])

const audioPlaying = ref(false)
const selectedOption = ref('')
const audioElement = ref(null)

const currentSlide = computed(() => {
  if (props.slides.length === 0) return null
  return props.slides[Math.max(0, Math.min(props.currentIndex, props.slides.length - 1))]
})

const hasNext = computed(() => props.currentIndex < props.slides.length - 1)
const hasPrev = computed(() => props.currentIndex > 0)

const isQuizSlide = computed(() => currentSlide.value?.type === 'quiz')
const currentExercise = computed(() => currentSlide.value?.exercise)
const currentFeedback = computed(() => {
  if (!currentExercise.value) return null
  return props.quizFeedback[currentExercise.value.id]
})
const currentAnswer = computed(() => {
  if (!currentExercise.value) return ''
  return props.quizAnswers[currentExercise.value.id] || ''
})

const currentAudioUrl = computed(() => currentSlide.value?.audioUrl)

const slideContentHtml = computed(() => {
  if (!currentSlide.value?.content) return ''
  return marked(currentSlide.value.content)
})

const showNextChapterButton = computed(() => {
  return !hasNext.value && props.hasNextChapter
})

const nextSlide = () => {
  if (hasNext.value) {
    const newIndex = props.currentIndex + 1
    emit('update:currentIndex', newIndex)
    emit('slideChange', newIndex)
  } else {
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
  if (!audioElement.value) return
  if (audioPlaying.value) {
    audioElement.value.pause()
    audioPlaying.value = false
  } else {
    audioElement.value.play()
    audioPlaying.value = true
  }
}

const handleSlideClick = (event) => {
  if (isQuizSlide.value) return

  const rect = event.currentTarget.getBoundingClientRect()
  const clickX = event.clientX - rect.left
  if (clickX < rect.width * 0.3) {
    prevSlide()
  } else if (clickX > rect.width * 0.7) {
    nextSlide()
  }
}

const handleOptionChange = (option) => {
  if (!currentExercise.value) return
  selectedOption.value = option
  emit('updateQuizAnswer', {
    exerciseId: currentExercise.value.id,
    value: option
  })

  emit('submitQuizAnswer', {
    exerciseId: currentExercise.value.id,
    answer: option
  })
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

const setupKeyboardListener = () => {
  window.addEventListener('keydown', handleKeydown)
  return () => {
    window.removeEventListener('keydown', handleKeydown)
  }
}

setupKeyboardListener()

watch(() => props.currentIndex, () => {
  audioPlaying.value = false
  selectedOption.value = ''
})

watch(() => currentAnswer.value, (newAnswer) => {
  selectedOption.value = newAnswer
})

watch(() => currentAudioUrl.value, (url) => {
  if (audioElement.value) {
    audioElement.value.pause()
    audioPlaying.value = false
  }

  if (url) {
    setTimeout(() => {
      if (audioElement.value) {
        audioElement.value.play().then(() => {
          audioPlaying.value = true
        }).catch(() => {
          audioPlaying.value = false
        })
      }
    }, 100)
  }
})

const handleAudioEnded = () => {
  audioPlaying.value = false
}

const handleAudioPlay = () => {
  audioPlaying.value = true
}

const handleAudioPause = () => {
  audioPlaying.value = false
}
</script>

<template>
  <div class="slide-viewer">
    <div class="slide-header">
      <h2>{{ title }}</h2>
      <button
        v-if="currentAudioUrl"
        class="audio-toggle"
        @click="toggleAudio"
        :class="{ playing: audioPlaying }"
      >
        {{ audioPlaying ? '🔊' : '🔈' }}
      </button>
    </div>

    <audio
      v-if="currentAudioUrl"
      ref="audioElement"
      :src="currentAudioUrl"
      @ended="handleAudioEnded"
      @play="handleAudioPlay"
      @pause="handleAudioPause"
      style="display: none;"
    ></audio>

    <div class="slide-container">
      <div
        v-if="currentSlide"
        class="slide-card"
        @click="handleSlideClick"
      >
        <div class="slide-content">
          <div v-if="currentSlide.content" v-html="slideContentHtml"></div>

          <QuizOptions
            v-if="isQuizSlide && currentExercise"
            :exercise="currentExercise"
            :selected-option="selectedOption"
            :feedback="currentFeedback"
            @select="handleOptionChange"
          />

          <QuizFeedback
            v-if="isQuizSlide && currentFeedback"
            :feedback="currentFeedback"
          />
        </div>
      </div>

      <div v-else class="empty-slide">
        <p>暂无幻灯片内容</p>
      </div>
    </div>

    <SlideNavigation
      :current-index="currentIndex"
      :total="slides.length"
      :has-next="hasNext"
      :has-prev="hasPrev"
      :show-next-chapter="showNextChapterButton"
      @prev="prevSlide"
      @next="nextSlide"
      @next-chapter="emit('nextChapter')"
    />
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
  margin-bottom: 15px;
}

.slide-header h2 {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}

.audio-toggle {
  background: none;
  border: none;
  font-size: 16px;
  cursor: pointer;
  padding: 0;
  width: 30px;
  height: 30px;
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
  flex: 0 1 auto;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  margin-bottom: 20px;
  position: relative;
  overflow-y: auto;
  max-height: 100%;
}

.slide-card {
  width: 100%;
  max-width: 90vw;
  min-height: 60vh;
  background: white;
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
  padding: 25px;
  margin: 20px;
  cursor: pointer;
  transition: transform 0.3s, box-shadow 0.3s;
  position: relative;
}

@media (min-height: 600px) {
  .slide-card {
    max-height: 80vh;
    overflow-y: auto;
  }
}

.slide-content {
  font-size: 16px;
  line-height: 1.5;
}

.slide-content h1, .slide-content h2, .slide-content h3 {
  margin-top: 0;
  margin-bottom: 15px;
}

.slide-content p {
  margin-bottom: 15px;
}

.slide-content a {
  color: #007bff;
  text-decoration: underline;
  cursor: pointer;
}

.slide-content a:hover {
  color: #0056b3;
}

.slide-content img {
  max-width: 100%;
  height: auto;
  display: block;
  margin: 0 auto;
}

.slide-content table {
  max-width: 100%;
  overflow-x: auto;
  display: block;
}

.slide-content pre {
  max-width: 100%;
  overflow-x: auto;
  white-space: pre-wrap;
  word-wrap: break-word;
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
