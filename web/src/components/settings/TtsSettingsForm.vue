<template>
  <form @submit.prevent="$emit('submit')" class="settings-form">
    <!-- 启用 TTS 开关 -->
    <div class="form-group">
      <label for="tts-enabled" class="switch-label">
        启用 TTS 功能
        <input
          id="tts-enabled"
          type="checkbox"
          :checked="formData.enabled"
          :disabled="saving"
          @change="$emit('update:enabled', $event.target.checked)"
        />
        <span class="switch-slider"></span>
      </label>
      <span class="hint">开启后将自动为学习内容生成语音讲解</span>
    </div>

    <!-- API Key -->
    <div class="form-group">
      <label for="tts-apiKey">
        API Key
        <span class="required">*</span>
      </label>
      <input
        id="tts-apiKey"
        :value="formData.apiKey"
        type="password"
        placeholder="sk-..."
        :disabled="saving"
        @input="$emit('update:apiKey', $event.target.value)"
        @blur="$emit('validate', 'apiKey')"
      />
      <span v-if="errors.apiKey" class="error">{{ errors.apiKey }}</span>
      <span class="hint">您的 TTS API 密钥（OpenAI 兼容格式）</span>
    </div>

    <!-- Base URL -->
    <div class="form-group">
      <label for="tts-baseUrl">
        Base URL
        <span class="required">*</span>
      </label>
      <select
        id="tts-baseUrlSelect"
        :value="formData.baseUrl"
        :disabled="saving"
        @change="handleBaseUrlChange"
      >
        <option value="https://api.openai.com/v1">OpenAI (api.openai.com)</option>
        <option value="custom">-- 自定义 URL --</option>
      </select>
      <input
        v-if="formData.baseUrl === 'custom'"
        id="tts-baseUrl"
        :value="customBaseUrl"
        type="url"
        placeholder="https://your-api-endpoint.com/v1"
        :disabled="saving"
        @input="$emit('update:customBaseUrl', $event.target.value)"
        @blur="$emit('validate', 'baseUrl')"
      />
      <span v-if="errors.baseUrl" class="error">{{ errors.baseUrl }}</span>
      <span class="hint">TTS API 端点（OpenAI 兼容格式）</span>
    </div>

    <!-- Voice -->
    <div class="form-group">
      <label for="tts-voice">
        Voice
        <span class="required">*</span>
      </label>
      <select
        id="tts-voice"
        :value="formData.voice"
        :disabled="saving"
        @change="handleVoiceChange"
      >
        <optgroup label="OpenAI 语音">
          <option value="alloy">Alloy</option>
          <option value="echo">Echo</option>
          <option value="fable">Fable</option>
          <option value="onyx">Onyx</option>
          <option value="nova">Nova</option>
          <option value="shimmer">Shimmer</option>
        </optgroup>
        <option value="custom">-- 自定义语音 --</option>
      </select>
      <input
        v-if="formData.voice === 'custom'"
        id="tts-customVoice"
        :value="customVoice"
        type="text"
        placeholder="voice-name"
        :disabled="saving"
        @input="$emit('update:customVoice', $event.target.value)"
      />
      <span v-if="errors.voice" class="error">{{ errors.voice }}</span>
      <span class="hint">选择 TTS 语音模型</span>
    </div>

    <!-- Speed -->
    <div class="form-group">
      <label for="tts-speed">
        语速
      </label>
      <div class="speed-control">
        <input
          id="tts-speed"
          :value="formData.speed"
          type="range"
          min="0.25"
          max="4.0"
          step="0.25"
          :disabled="saving"
          @input="$emit('update:speed', parseFloat($event.target.value))"
        />
        <span class="speed-value">{{ formData.speed.toFixed(2) }}x</span>
      </div>
      <span class="hint">控制播放速度（0.25x 最慢 - 4.0x 最快，默认 1.0x）</span>
    </div>
  </form>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  formData: {
    type: Object,
    required: true,
  },
  customBaseUrl: {
    type: String,
    default: '',
  },
  customVoice: {
    type: String,
    default: '',
  },
  errors: {
    type: Object,
    default: () => ({}),
  },
  saving: {
    type: Boolean,
    default: false,
  },
})

const emit = defineEmits([
  'update:enabled',
  'update:apiKey',
  'update:customBaseUrl',
  'update:customVoice',
  'update:speed',
  'baseUrlChange',
  'voiceChange',
  'validate',
  'submit',
])

const isFormValid = computed(() => {
  const baseUrl = props.formData.baseUrl === 'custom' ? props.customBaseUrl : props.formData.baseUrl
  const voice = props.formData.voice === 'custom' ? props.customVoice : props.formData.voice
  return (
    props.formData.apiKey.trim() !== '' &&
    baseUrl.trim() !== '' &&
    voice.trim() !== '' &&
    !props.errors.apiKey &&
    !props.errors.baseUrl &&
    !props.errors.voice
  )
})

const handleBaseUrlChange = (event) => {
  emit('baseUrlChange', event.target.value)
}

const handleVoiceChange = (event) => {
  emit('voiceChange', event.target.value)
}
</script>

<style scoped>
.settings-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-group label {
  font-size: 13px;
  font-weight: 500;
  color: #374151;
}

.required {
  color: #ef4444;
  margin-left: 2px;
}

.form-group input,
.form-group select {
  border: 1px solid #d7dbe6;
  padding: 10px 12px;
  border-radius: 8px;
  font-size: 14px;
  transition: border-color 0.2s;
}

.form-group input:focus,
.form-group select:focus {
  outline: none;
  border-color: #3772ff;
  box-shadow: 0 0 0 3px rgba(55, 114, 255, 0.1);
}

.form-group input:disabled,
.form-group select:disabled {
  background: #f9fafb;
  cursor: not-allowed;
}

.error {
  color: #ef4444;
  font-size: 12px;
}

.hint {
  color: #6b7280;
  font-size: 12px;
}

.speed-control {
  display: flex;
  align-items: center;
  gap: 12px;
}

.speed-control input[type="range"] {
  flex: 1;
  padding: 0;
}

.speed-value {
  font-size: 13px;
  font-weight: 500;
  color: #3772ff;
  min-width: 50px;
  text-align: center;
}

/* 开关样式 */
.switch-label {
  display: flex;
  align-items: center;
  justify-content: space-between;
  cursor: pointer;
}

.switch-label input[type="checkbox"] {
  display: none;
}

.switch-slider {
  position: relative;
  width: 44px;
  height: 24px;
  background-color: #d1d5db;
  border-radius: 24px;
  transition: background-color 0.2s;
}

.switch-slider::before {
  content: '';
  position: absolute;
  width: 18px;
  height: 18px;
  left: 3px;
  top: 3px;
  background-color: white;
  border-radius: 50%;
  transition: transform 0.2s;
}

.switch-label input:checked + .switch-slider {
  background-color: #3772ff;
}

.switch-label input:checked + .switch-slider::before {
  transform: translateX(20px);
}
</style>
