<template>
  <div v-if="open" class="settings-overlay" @click.self="$emit('close')">
    <div class="settings-dialog">
      <div class="dialog-header">
        <h2>系统配置设置</h2>
        <button type="button" class="close-btn" @click="$emit('close')">×</button>
      </div>

      <!-- Tab 切换 -->
      <div class="dialog-tabs">
        <button
          type="button"
          :class="['tab-btn', { active: activeTab === 'llm' }]"
          @click="activeTab = 'llm'"
        >
          LLM 配置
        </button>
        <button
          type="button"
          :class="['tab-btn', { active: activeTab === 'tts' }]"
          @click="activeTab = 'tts'"
        >
          TTS 配置
        </button>
      </div>

      <div class="dialog-body">
        <!-- LLM 设置表单 -->
        <LlmSettingsForm
          v-if="activeTab === 'llm'"
          :form-data="llmFormData"
          :selected-provider="llmSelectedProvider"
          :custom-base-url="llmCustomBaseUrl"
          :custom-model="llmCustomModel"
          :errors="llmErrors"
          :testing="llmTesting"
          :test-result="llmTestResult"
          :provider-presets="llmProviderPresets"
          :preset-base-urls="llmPresetBaseUrls"
          @update:api-key="llmFormData.apiKey = $event"
          @update:custom-base-url="llmCustomBaseUrl = $event"
          @update:custom-model="llmCustomModel = $event"
          @update:concurrency="llmFormData.concurrency = $event"
          @provider-change="handleLlmProviderChange"
          @base-url-change="handleLlmBaseUrlChange"
          @model-change="handleLlmModelChange"
          @validate="validateLlmField"
          @test="handleLlmTest"
          @submit="handleLlmSubmit"
        />

        <!-- TTS 设置表单 -->
        <TtsSettingsForm
          v-if="activeTab === 'tts'"
          :form-data="ttsFormData"
          :custom-base-url="ttsCustomBaseUrl"
          :custom-voice="ttsCustomVoice"
          :errors="ttsErrors"
          :saving="ttsSaving"
          @update:enabled="ttsFormData.enabled = $event"
          @update:api-key="ttsFormData.apiKey = $event"
          @update:custom-base-url="ttsCustomBaseUrl = $event"
          @update:custom-voice="ttsCustomVoice = $event"
          @update:speed="ttsFormData.speed = $event"
          @base-url-change="handleTtsBaseUrlChange"
          @voice-change="handleTtsVoiceChange"
          @validate="validateTtsField"
          @submit="handleTtsSubmit"
        />
      </div>

      <div class="dialog-footer">
        <button type="button" class="btn btn-secondary" @click="$emit('close')">
          取消
        </button>
        <button
          type="button"
          class="btn btn-primary"
          :disabled="isSaving"
          @click="handleSave"
        >
          <span v-if="isSaving">保存中...</span>
          <span v-else>保存</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { getLlmSettings, updateLlmSettings, testLlmConnection, getTtsSettings, updateTtsSettings } from '../../api'
import LlmSettingsForm from './LlmSettingsForm.vue'
import TtsSettingsForm from './TtsSettingsForm.vue'

const props = defineProps({
  open: {
    type: Boolean,
    default: false,
  },
})

const emit = defineEmits(['close', 'saved'])

// 当前激活的标签页
const activeTab = ref('llm')

// ==================== LLM 配置 ====================
const llmPresetBaseUrls = [
  'https://api.openai.com/v1',
  'https://api.deepseek.com/v1',
  'https://dashscope.aliyuncs.com/compatible-mode/v1',
  'http://localhost:11434/v1',
]

const llmProviderPresets = {
  openai: {
    baseUrl: 'https://api.openai.com/v1',
    model: 'gpt-4o',
    apiKeyPlaceholder: 'sk-...',
    apiKeyHint: '您的 OpenAI API 密钥',
  },
  deepseek: {
    baseUrl: 'https://api.deepseek.com/v1',
    model: 'deepseek-chat',
    apiKeyPlaceholder: 'sk-...',
    apiKeyHint: '您的 DeepSeek API 密钥',
  },
  qwen: {
    baseUrl: 'https://dashscope.aliyuncs.com/compatible-mode/v1',
    model: 'qwen-turbo',
    apiKeyPlaceholder: 'sk-...',
    apiKeyHint: '您的阿里云 DashScope API 密钥',
  },
  ollama: {
    baseUrl: 'http://localhost:11434/v1',
    model: 'llama3.2',
    apiKeyPlaceholder: 'ollama',
    apiKeyHint: 'Ollama 本地服务，通常填写 "ollama"',
  },
}

const llmBaseUrlToProvider = {
  'https://api.openai.com/v1': 'openai',
  'https://api.deepseek.com/v1': 'deepseek',
  'https://dashscope.aliyuncs.com/compatible-mode/v1': 'qwen',
  'http://localhost:11434/v1': 'ollama',
}

const llmFormData = reactive({
  apiKey: '',
  baseUrl: 'https://api.openai.com/v1',
  model: 'gpt-4o',
  concurrency: 1,
})

const llmSelectedProvider = ref('')
const llmCustomBaseUrl = ref('')
const llmCustomModel = ref('')

const llmErrors = reactive({
  apiKey: '',
  baseUrl: '',
  model: '',
  concurrency: '',
})

const llmTesting = ref(false)
const llmTestResult = ref(null)

const llmActualBaseUrl = computed(() => {
  if (llmFormData.baseUrl === 'custom') {
    return llmCustomBaseUrl.value
  }
  return llmFormData.baseUrl
})

const llmActualModel = computed(() => {
  if (llmFormData.model === 'custom') {
    return llmCustomModel.value
  }
  return llmFormData.model
})

const handleLlmProviderChange = (value) => {
  llmSelectedProvider.value = value
  if (value && llmProviderPresets[value]) {
    const preset = llmProviderPresets[value]
    llmFormData.baseUrl = preset.baseUrl
    llmFormData.model = preset.model
    llmCustomBaseUrl.value = ''
    llmCustomModel.value = ''
  }
}

const handleLlmBaseUrlChange = (value) => {
  llmFormData.baseUrl = value
  if (llmBaseUrlToProvider[value]) {
    llmSelectedProvider.value = llmBaseUrlToProvider[value]
  }
  validateLlmField('baseUrl')
}

const handleLlmModelChange = (value) => {
  llmFormData.model = value
  validateLlmField('model')
}

const validateLlmField = (field) => {
  llmErrors[field] = ''

  if (field === 'apiKey' && !llmFormData.apiKey.trim()) {
    llmErrors.apiKey = 'API Key 不能为空'
  }

  if (field === 'baseUrl') {
    const url = llmActualBaseUrl.value
    if (!url.trim()) {
      llmErrors.baseUrl = 'Base URL 不能为空'
    } else if (!url.startsWith('http')) {
      llmErrors.baseUrl = '请输入有效的 URL'
    }
  }

  if (field === 'model') {
    const model = llmActualModel.value
    if (!model.trim()) {
      llmErrors.model = 'Model 不能为空'
    }
  }

  if (field === 'concurrency') {
    const value = llmFormData.concurrency
    if (!value || value < 1 || value > 10) {
      llmErrors.concurrency = '并发数必须在 1-10 之间'
    }
  }
}

const handleLlmTest = async () => {
  llmTesting.value = true
  llmTestResult.value = null

  try {
    const data = await testLlmConnection({
      apiKey: llmFormData.apiKey,
      baseUrl: llmActualBaseUrl.value,
      model: llmActualModel.value,
    })

    const result = data.items?.[0] || data
    llmTestResult.value = {
      success: true,
      message: result.message || '连接成功',
      responseTimeMs: result.responseTimeMs,
    }
  } catch (error) {
    llmTestResult.value = {
      success: false,
      message: error.message || '连接测试失败',
    }
  } finally {
    llmTesting.value = false
  }
}

const handleLlmSubmit = () => {}

// ==================== TTS 配置 ====================
const ttsFormData = reactive({
  enabled: true,
  apiKey: '',
  baseUrl: 'https://api.openai.com/v1',
  voice: 'alloy',
  speed: 1.0,
})

const ttsCustomBaseUrl = ref('')
const ttsCustomVoice = ref('')

const ttsErrors = reactive({
  apiKey: '',
  baseUrl: '',
  voice: '',
})

const ttsSaving = ref(false)

const ttsPresetBaseUrls = ['https://api.openai.com/v1']

const ttsActualBaseUrl = computed(() => {
  if (ttsFormData.baseUrl === 'custom') {
    return ttsCustomBaseUrl.value
  }
  return ttsFormData.baseUrl
})

const ttsActualVoice = computed(() => {
  if (ttsFormData.voice === 'custom') {
    return ttsCustomVoice.value
  }
  return ttsFormData.voice
})

const handleTtsBaseUrlChange = (value) => {
  ttsFormData.baseUrl = value
  validateTtsField('baseUrl')
}

const handleTtsVoiceChange = (value) => {
  ttsFormData.voice = value
  validateTtsField('voice')
}

const validateTtsField = (field) => {
  ttsErrors[field] = ''

  if (field === 'apiKey' && !ttsFormData.apiKey.trim()) {
    ttsErrors.apiKey = 'API Key 不能为空'
  }

  if (field === 'baseUrl') {
    const url = ttsActualBaseUrl.value
    if (!url.trim()) {
      ttsErrors.baseUrl = 'Base URL 不能为空'
    } else if (!url.startsWith('http')) {
      ttsErrors.baseUrl = '请输入有效的 URL'
    }
  }

  if (field === 'voice') {
    const voice = ttsActualVoice.value
    if (!voice.trim()) {
      ttsErrors.voice = 'Voice 不能为空'
    }
  }
}

const handleTtsSubmit = () => {}

// ==================== 通用方法 ====================
const isSaving = computed(() => {
  if (activeTab.value === 'llm') {
    return llmTesting.value
  }
  return ttsSaving.value
})

const loadSettings = async () => {
  try {
    const llmData = await getLlmSettings()
    const llmSettings = llmData.items?.[0]
    if (llmSettings) {
      const loadedUrl = llmSettings.baseUrl || 'https://api.openai.com/v1'
      const loadedModel = llmSettings.model || 'gpt-4o'
      const loadedConcurrency = llmSettings.concurrency ?? 1

      if (llmPresetBaseUrls.includes(loadedUrl)) {
        llmFormData.baseUrl = loadedUrl
        llmSelectedProvider.value = llmBaseUrlToProvider[loadedUrl] || ''
      } else {
        llmFormData.baseUrl = 'custom'
        llmCustomBaseUrl.value = loadedUrl
      }

      llmFormData.model = loadedModel
      llmFormData.concurrency = loadedConcurrency
    }

    const ttsData = await getTtsSettings()
    const ttsSettings = ttsData.items?.[0]
    if (ttsSettings) {
      const loadedUrl = ttsSettings.baseUrl || 'https://api.openai.com/v1'
      const loadedVoice = ttsSettings.voice || 'alloy'
      const loadedSpeed = ttsSettings.speed || 1.0
      const loadedEnabled = ttsSettings.enabled !== undefined ? ttsSettings.enabled : true

      if (ttsPresetBaseUrls.includes(loadedUrl)) {
        ttsFormData.baseUrl = loadedUrl
      } else {
        ttsFormData.baseUrl = 'custom'
        ttsCustomBaseUrl.value = loadedUrl
      }

      ttsFormData.voice = loadedVoice
      ttsFormData.speed = loadedSpeed
      ttsFormData.enabled = loadedEnabled
    }
  } catch (error) {
    console.error('加载配置失败:', error)
  }
}

const handleSave = async () => {
  if (activeTab.value === 'llm') {
    llmTesting.value = true

    try {
      await updateLlmSettings({
        apiKey: llmFormData.apiKey,
        baseUrl: llmActualBaseUrl.value,
        model: llmActualModel.value,
        concurrency: llmFormData.concurrency,
      })

      emit('saved')
      emit('close')
    } catch (error) {
      llmTestResult.value = {
        success: false,
        message: error.message || '保存失败',
      }
    } finally {
      llmTesting.value = false
    }
  } else {
    ttsSaving.value = true

    try {
      await updateTtsSettings({
        enabled: ttsFormData.enabled,
        apiKey: ttsFormData.apiKey,
        baseUrl: ttsActualBaseUrl.value,
        voice: ttsActualVoice.value,
        speed: ttsFormData.speed,
      })

      emit('saved')
      emit('close')
    } catch (error) {
      console.error('TTS 配置保存失败:', error)
      alert(error.message || '保存失败')
    } finally {
      ttsSaving.value = false
    }
  }
}

watch(
  () => props.open,
  (isOpen) => {
    if (isOpen) {
      loadSettings()
      llmTestResult.value = null
    } else {
      llmFormData.apiKey = ''
      llmSelectedProvider.value = ''
      llmCustomBaseUrl.value = ''
      llmCustomModel.value = ''
      llmFormData.concurrency = 1
      ttsFormData.enabled = true
      ttsFormData.apiKey = ''
      ttsCustomBaseUrl.value = ''
      ttsCustomVoice.value = ''
      ttsFormData.speed = 1.0
    }
  },
)
</script>

<style scoped>
.settings-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.settings-dialog {
  background: #fff;
  border-radius: 12px;
  width: 90%;
  max-width: 500px;
  max-height: 90vh;
  overflow: auto;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
}

.dialog-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 24px;
  border-bottom: 1px solid #e5e7eb;
}

.dialog-header h2 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}

.close-btn {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #6b7280;
  padding: 0;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
}

.close-btn:hover {
  background: #f3f4f6;
}

.dialog-tabs {
  display: flex;
  border-bottom: 1px solid #e5e7eb;
  padding: 0 24px;
}

.tab-btn {
  background: none;
  border: none;
  padding: 14px 16px;
  font-size: 14px;
  font-weight: 500;
  color: #6b7280;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  transition: all 0.2s;
}

.tab-btn:hover {
  color: #374151;
}

.tab-btn.active {
  color: #3772ff;
  border-bottom-color: #3772ff;
}

.dialog-body {
  padding: 24px;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 24px;
  border-top: 1px solid #e5e7eb;
  background: #f9fafb;
}

.btn {
  padding: 10px 20px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  border: none;
  transition: all 0.2s;
}

.btn-secondary {
  background: #fff;
  border: 1px solid #d7dbe6;
  color: #374151;
}

.btn-secondary:hover {
  background: #f9fafb;
}

.btn-primary {
  background: #3772ff;
  color: #fff;
}

.btn-primary:hover:not(:disabled) {
  background: #2b5cdc;
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
