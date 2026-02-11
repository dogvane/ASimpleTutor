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
        <form v-if="activeTab === 'llm'" @submit.prevent="handleLlmSubmit" class="settings-form">
          <!-- 服务商选择 -->
          <div class="form-group">
            <label for="llm-provider">服务商</label>
            <select
              id="llm-provider"
              v-model="llmSelectedProvider"
              :disabled="llmTesting"
              @change="handleLlmProviderChange"
            >
              <option value="">-- 自定义 --</option>
              <option value="openai">OpenAI</option>
              <option value="deepseek">DeepSeek</option>
              <option value="qwen">通义千问 (Qwen)</option>
              <option value="ollama">Ollama (本地)</option>
            </select>
            <span class="hint">选择服务商自动填充配置</span>
          </div>

          <!-- API Key -->
          <div class="form-group">
            <label for="llm-apiKey">
              API Key
              <span class="required">*</span>
            </label>
            <input
              id="llm-apiKey"
              v-model="llmFormData.apiKey"
              type="password"
              :placeholder="llmApiKeyPlaceholder"
              :disabled="llmTesting"
              @blur="validateLlmField('apiKey')"
            />
            <span v-if="llmErrors.apiKey" class="error">{{ llmErrors.apiKey }}</span>
            <span class="hint">{{ llmApiKeyHint }}</span>
          </div>

          <!-- Base URL -->
          <div class="form-group">
            <label for="llm-baseUrl">
              Base URL
              <span class="required">*</span>
            </label>
            <select
              id="llm-baseUrlSelect"
              v-model="llmFormData.baseUrl"
              :disabled="llmTesting"
              @change="handleLlmBaseUrlChange"
            >
              <option value="https://api.openai.com/v1">OpenAI (api.openai.com)</option>
              <option value="https://api.deepseek.com/v1">DeepSeek (api.deepseek.com)</option>
              <option value="https://dashscope.aliyuncs.com/compatible-mode/v1">通义千问 (dashscope.aliyuncs.com)</option>
              <option value="http://localhost:11434/v1">Ollama 本地 (localhost:11434)</option>
              <option value="custom">-- 自定义 URL --</option>
            </select>
            <input
              v-if="llmFormData.baseUrl === 'custom' || !llmIsPresetBaseUrl"
              id="llm-baseUrl"
              v-model="llmCustomBaseUrl"
              type="url"
              placeholder="https://your-api-endpoint.com/v1"
              :disabled="llmTesting"
              @blur="validateLlmField('baseUrl')"
            />
            <span v-if="llmErrors.baseUrl" class="error">{{ llmErrors.baseUrl }}</span>
          </div>

          <!-- Model -->
          <div class="form-group">
            <label for="llm-model">
              Model
              <span class="required">*</span>
            </label>
            <select
              id="llm-model"
              v-model="llmFormData.model"
              :disabled="llmTesting"
              @change="validateLlmField('model')"
            >
              <optgroup v-if="llmCurrentProvider === 'openai' || !llmCurrentProvider" label="OpenAI">
                <option value="gpt-4o">GPT-4o</option>
                <option value="gpt-4o-mini">GPT-4o Mini</option>
                <option value="gpt-4-turbo">GPT-4 Turbo</option>
                <option value="gpt-4">GPT-4</option>
                <option value="gpt-3.5-turbo">GPT-3.5 Turbo</option>
              </optgroup>
              <optgroup v-if="llmCurrentProvider === 'deepseek' || !llmCurrentProvider" label="DeepSeek">
                <option value="deepseek-chat">DeepSeek Chat</option>
                <option value="deepseek-coder">DeepSeek Coder</option>
                <option value="deepseek-reasoner">DeepSeek Reasoner</option>
              </optgroup>
              <optgroup v-if="llmCurrentProvider === 'qwen' || !llmCurrentProvider" label="通义千问">
                <option value="qwen-turbo">Qwen Turbo</option>
                <option value="qwen-plus">Qwen Plus</option>
                <option value="qwen-max">Qwen Max</option>
                <option value="qwen-long">Qwen Long</option>
              </optgroup>
              <optgroup v-if="llmCurrentProvider === 'ollama' || !llmCurrentProvider" label="Ollama">
                <option value="llama3.2">Llama 3.2</option>
                <option value="llama3.1">Llama 3.1</option>
                <option value="llama3">Llama 3</option>
                <option value="qwen2.5">Qwen 2.5</option>
                <option value="deepseek-v2">DeepSeek V2</option>
              </optgroup>
              <option value="custom">-- 自定义模型 --</option>
            </select>
            <input
              v-if="llmFormData.model === 'custom'"
              id="llm-customModel"
              v-model="llmCustomModel"
              type="text"
              placeholder="model-name"
              :disabled="llmTesting"
            />
            <span v-if="llmErrors.model" class="error">{{ llmErrors.model }}</span>
          </div>

          <!-- Test Connection -->
          <div class="form-group">
            <button
              type="button"
              class="test-btn"
              :disabled="llmTesting || !llmIsFormValid"
              @click="handleLlmTest"
            >
              <span v-if="llmTesting">测试中...</span>
              <span v-else>测试连接</span>
            </button>
            <span
              v-if="llmTestResult"
              :class="['test-result', llmTestResult.success ? 'success' : 'error']"
            >
              {{ llmTestResult.message }}
              <span v-if="llmTestResult.responseTimeMs" class="response-time">
                ({{ llmTestResult.responseTimeMs }}ms)
              </span>
            </span>
          </div>
        </form>

        <!-- TTS 设置表单 -->
        <form v-if="activeTab === 'tts'" @submit.prevent="handleTtsSubmit" class="settings-form">
          <!-- API Key -->
          <div class="form-group">
            <label for="tts-apiKey">
              API Key
              <span class="required">*</span>
            </label>
            <input
              id="tts-apiKey"
              v-model="ttsFormData.apiKey"
              type="password"
              placeholder="sk-..."
              :disabled="ttsSaving"
              @blur="validateTtsField('apiKey')"
            />
            <span v-if="ttsErrors.apiKey" class="error">{{ ttsErrors.apiKey }}</span>
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
              v-model="ttsFormData.baseUrl"
              :disabled="ttsSaving"
              @change="handleTtsBaseUrlChange"
            >
              <option value="https://api.openai.com/v1">OpenAI (api.openai.com)</option>
              <option value="custom">-- 自定义 URL --</option>
            </select>
            <input
              v-if="ttsFormData.baseUrl === 'custom'"
              id="tts-baseUrl"
              v-model="ttsCustomBaseUrl"
              type="url"
              placeholder="https://your-api-endpoint.com/v1"
              :disabled="ttsSaving"
              @blur="validateTtsField('baseUrl')"
            />
            <span v-if="ttsErrors.baseUrl" class="error">{{ ttsErrors.baseUrl }}</span>
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
              v-model="ttsFormData.voice"
              :disabled="ttsSaving"
              @change="validateTtsField('voice')"
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
              v-if="ttsFormData.voice === 'custom'"
              id="tts-customVoice"
              v-model="ttsCustomVoice"
              type="text"
              placeholder="voice-name"
              :disabled="ttsSaving"
            />
            <span v-if="ttsErrors.voice" class="error">{{ ttsErrors.voice }}</span>
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
                v-model.number="ttsFormData.speed"
                type="range"
                min="0.25"
                max="4.0"
                step="0.25"
                :disabled="ttsSaving"
              />
              <span class="speed-value">{{ ttsFormData.speed.toFixed(2) }}x</span>
            </div>
            <span class="hint">控制播放速度（0.25x 最慢 - 4.0x 最快，默认 1.0x）</span>
          </div>
        </form>
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
import { getLlmSettings, updateLlmSettings, testLlmConnection, getTtsSettings, updateTtsSettings } from '../api'

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
// 预设的 Base URL 列表
const llmPresetBaseUrls = [
  'https://api.openai.com/v1',
  'https://api.deepseek.com/v1',
  'https://dashscope.aliyuncs.com/compatible-mode/v1',
  'http://localhost:11434/v1',
]

// 服务商预设配置
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

// Base URL 到服务商的映射
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
})

const llmSelectedProvider = ref('')
const llmCustomBaseUrl = ref('')
const llmCustomModel = ref('')

const llmErrors = reactive({
  apiKey: '',
  baseUrl: '',
  model: '',
})

const llmTesting = ref(false)
const llmTestResult = ref(null)

// 当前服务商
const llmCurrentProvider = computed(() => {
  return llmBaseUrlToProvider[llmFormData.baseUrl] || llmSelectedProvider.value || ''
})

// 是否是预设的 Base URL
const llmIsPresetBaseUrl = computed(() => {
  return llmPresetBaseUrls.includes(llmFormData.baseUrl)
})

const llmApiKeyPlaceholder = computed(() => {
  const provider = llmCurrentProvider.value
  if (provider && llmProviderPresets[provider]) {
    return llmProviderPresets[provider].apiKeyPlaceholder
  }
  return 'sk-...'
})

const llmApiKeyHint = computed(() => {
  const provider = llmCurrentProvider.value
  if (provider && llmProviderPresets[provider]) {
    return llmProviderPresets[provider].apiKeyHint
  }
  return '您的 API 密钥将安全保存'
})

// 实际使用的 Base URL
const llmActualBaseUrl = computed(() => {
  if (llmFormData.baseUrl === 'custom') {
    return llmCustomBaseUrl.value
  }
  return llmFormData.baseUrl
})

// 实际使用的 Model
const llmActualModel = computed(() => {
  if (llmFormData.model === 'custom') {
    return llmCustomModel.value
  }
  return llmFormData.model
})

const llmIsFormValid = computed(() => {
  const baseUrl = llmActualBaseUrl.value
  const model = llmActualModel.value
  return (
    llmFormData.apiKey.trim() !== '' &&
    baseUrl.trim() !== '' &&
    model.trim() !== '' &&
    !llmErrors.apiKey &&
    !llmErrors.baseUrl &&
    !llmErrors.model
  )
})

// 服务商变化处理
const handleLlmProviderChange = () => {
  if (llmSelectedProvider.value && llmProviderPresets[llmSelectedProvider.value]) {
    const preset = llmProviderPresets[llmSelectedProvider.value]
    llmFormData.baseUrl = preset.baseUrl
    llmFormData.model = preset.model
    llmCustomBaseUrl.value = ''
    llmCustomModel.value = ''
  }
}

// Base URL 下拉框变化处理
const handleLlmBaseUrlChange = () => {
  if (llmBaseUrlToProvider[llmFormData.baseUrl]) {
    llmSelectedProvider.value = llmBaseUrlToProvider[llmFormData.baseUrl]
  }
  validateLlmField('baseUrl')
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

// ==================== TTS 配置 ====================
const ttsFormData = reactive({
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

// TTS 预设 Base URL
const ttsPresetBaseUrls = ['https://api.openai.com/v1']

// 实际使用的 Base URL
const ttsActualBaseUrl = computed(() => {
  if (ttsFormData.baseUrl === 'custom') {
    return ttsCustomBaseUrl.value
  }
  return ttsFormData.baseUrl
})

// 实际使用的 Voice
const ttsActualVoice = computed(() => {
  if (ttsFormData.voice === 'custom') {
    return ttsCustomVoice.value
  }
  return ttsFormData.voice
})

const ttsIsFormValid = computed(() => {
  const baseUrl = ttsActualBaseUrl.value
  const voice = ttsActualVoice.value
  return (
    ttsFormData.apiKey.trim() !== '' &&
    baseUrl.trim() !== '' &&
    voice.trim() !== '' &&
    !ttsErrors.apiKey &&
    !ttsErrors.baseUrl &&
    !ttsErrors.voice
  )
})

// Base URL 下拉框变化处理
const handleTtsBaseUrlChange = () => {
  validateTtsField('baseUrl')
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

// ==================== 通用方法 ====================
// 是否正在保存
const isSaving = computed(() => {
  if (activeTab.value === 'llm') {
    return llmTesting.value
  }
  return ttsSaving.value
})

const loadSettings = async () => {
  try {
    // 加载 LLM 配置
    const llmData = await getLlmSettings()
    const llmSettings = llmData.items?.[0]
    if (llmSettings) {
      const loadedUrl = llmSettings.baseUrl || 'https://api.openai.com/v1'
      const loadedModel = llmSettings.model || 'gpt-4o'

      if (llmPresetBaseUrls.includes(loadedUrl)) {
        llmFormData.baseUrl = loadedUrl
        llmSelectedProvider.value = llmBaseUrlToProvider[loadedUrl] || ''
      } else {
        llmFormData.baseUrl = 'custom'
        llmCustomBaseUrl.value = loadedUrl
      }

      llmFormData.model = loadedModel
    }

    // 加载 TTS 配置
    const ttsData = await getTtsSettings()
    const ttsSettings = ttsData.items?.[0]
    if (ttsSettings) {
      const loadedUrl = ttsSettings.baseUrl || 'https://api.openai.com/v1'
      const loadedVoice = ttsSettings.voice || 'alloy'
      const loadedSpeed = ttsSettings.speed || 1.0

      if (ttsPresetBaseUrls.includes(loadedUrl)) {
        ttsFormData.baseUrl = loadedUrl
      } else {
        ttsFormData.baseUrl = 'custom'
        ttsCustomBaseUrl.value = loadedUrl
      }

      ttsFormData.voice = loadedVoice
      ttsFormData.speed = loadedSpeed
    }
  } catch (error) {
    console.error('加载配置失败:', error)
  }
}

const handleLlmSubmit = async (e) => {
  e.preventDefault()
}

const handleTtsSubmit = async (e) => {
  e.preventDefault()
}

const handleSave = async () => {
  if (activeTab.value === 'llm') {
    if (!llmIsFormValid.value) return

    llmTesting.value = true

    try {
      await updateLlmSettings({
        apiKey: llmFormData.apiKey,
        baseUrl: llmActualBaseUrl.value,
        model: llmActualModel.value,
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
    if (!ttsIsFormValid.value) return

    ttsSaving.value = true

    try {
      await updateTtsSettings({
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

// Reset form when dialog opens/closes
watch(
  () => props.open,
  (isOpen) => {
    if (isOpen) {
      loadSettings()
      llmTestResult.value = null
    } else {
      // Clear API keys when closing for security
      llmFormData.apiKey = ''
      llmSelectedProvider.value = ''
      llmCustomBaseUrl.value = ''
      llmCustomModel.value = ''
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

.test-btn {
  align-self: flex-start;
  border: 1px solid #d7dbe6;
  background: #fff;
  color: #374151;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
}

.test-btn:hover:not(:disabled) {
  border-color: #3772ff;
  color: #3772ff;
}

.test-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.test-result {
  font-size: 13px;
  margin-left: 12px;
}

.test-result.success {
  color: #059669;
}

.test-result.error {
  color: #dc2626;
}

.response-time {
  color: #6b7280;
  font-size: 12px;
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
