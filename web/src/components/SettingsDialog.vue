<template>
  <div v-if="open" class="settings-overlay" @click.self="$emit('close')">
    <div class="settings-dialog">
      <div class="dialog-header">
        <h2>LLM 配置设置</h2>
        <button type="button" class="close-btn" @click="$emit('close')">×</button>
      </div>

      <div class="dialog-body">
        <form @submit.prevent="handleSubmit" class="settings-form">
          <!-- 服务商选择 -->
          <div class="form-group">
            <label for="provider">服务商</label>
            <select
              id="provider"
              v-model="selectedProvider"
              :disabled="testing"
              @change="handleProviderChange"
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
            <label for="apiKey">
              API Key
              <span class="required">*</span>
            </label>
            <input
              id="apiKey"
              v-model="formData.apiKey"
              type="password"
              :placeholder="apiKeyPlaceholder"
              :disabled="testing"
              @blur="validateField('apiKey')"
            />
            <span v-if="errors.apiKey" class="error">{{ errors.apiKey }}</span>
            <span class="hint">{{ apiKeyHint }}</span>
          </div>

          <!-- Base URL -->
          <div class="form-group">
            <label for="baseUrl">
              Base URL
              <span class="required">*</span>
            </label>
            <select
              id="baseUrlSelect"
              v-model="formData.baseUrl"
              :disabled="testing"
              @change="handleBaseUrlChange"
            >
              <option value="https://api.openai.com/v1">OpenAI (api.openai.com)</option>
              <option value="https://api.deepseek.com/v1">DeepSeek (api.deepseek.com)</option>
              <option value="https://dashscope.aliyuncs.com/compatible-mode/v1">通义千问 (dashscope.aliyuncs.com)</option>
              <option value="http://localhost:11434/v1">Ollama 本地 (localhost:11434)</option>
              <option value="custom">-- 自定义 URL --</option>
            </select>
            <input
              v-if="formData.baseUrl === 'custom' || !isPresetBaseUrl"
              id="baseUrl"
              v-model="customBaseUrl"
              type="url"
              placeholder="https://your-api-endpoint.com/v1"
              :disabled="testing"
              @blur="validateField('baseUrl')"
            />
            <span v-if="errors.baseUrl" class="error">{{ errors.baseUrl }}</span>
          </div>

          <!-- Model -->
          <div class="form-group">
            <label for="model">
              Model
              <span class="required">*</span>
            </label>
            <select
              id="model"
              v-model="formData.model"
              :disabled="testing"
              @change="validateField('model')"
            >
              <optgroup v-if="currentProvider === 'openai' || !currentProvider" label="OpenAI">
                <option value="gpt-4o">GPT-4o</option>
                <option value="gpt-4o-mini">GPT-4o Mini</option>
                <option value="gpt-4-turbo">GPT-4 Turbo</option>
                <option value="gpt-4">GPT-4</option>
                <option value="gpt-3.5-turbo">GPT-3.5 Turbo</option>
              </optgroup>
              <optgroup v-if="currentProvider === 'deepseek' || !currentProvider" label="DeepSeek">
                <option value="deepseek-chat">DeepSeek Chat</option>
                <option value="deepseek-coder">DeepSeek Coder</option>
                <option value="deepseek-reasoner">DeepSeek Reasoner</option>
              </optgroup>
              <optgroup v-if="currentProvider === 'qwen' || !currentProvider" label="通义千问">
                <option value="qwen-turbo">Qwen Turbo</option>
                <option value="qwen-plus">Qwen Plus</option>
                <option value="qwen-max">Qwen Max</option>
                <option value="qwen-long">Qwen Long</option>
              </optgroup>
              <optgroup v-if="currentProvider === 'ollama' || !currentProvider" label="Ollama">
                <option value="llama3.2">Llama 3.2</option>
                <option value="llama3.1">Llama 3.1</option>
                <option value="llama3">Llama 3</option>
                <option value="qwen2.5">Qwen 2.5</option>
                <option value="deepseek-v2">DeepSeek V2</option>
              </optgroup>
              <option value="custom">-- 自定义模型 --</option>
            </select>
            <input
              v-if="formData.model === 'custom'"
              id="customModel"
              v-model="customModel"
              type="text"
              placeholder="model-name"
              :disabled="testing"
            />
            <span v-if="errors.model" class="error">{{ errors.model }}</span>
          </div>

          <!-- Test Connection -->
          <div class="form-group">
            <button
              type="button"
              class="test-btn"
              :disabled="testing || !isFormValid"
              @click="handleTest"
            >
              <span v-if="testing">测试中...</span>
              <span v-else>测试连接</span>
            </button>
            <span
              v-if="testResult"
              :class="['test-result', testResult.success ? 'success' : 'error']"
            >
              {{ testResult.message }}
              <span v-if="testResult.responseTimeMs" class="response-time">
                ({{ testResult.responseTimeMs }}ms)
              </span>
            </span>
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
          :disabled="saving || !isFormValid"
          @click="handleSave"
        >
          <span v-if="saving">保存中...</span>
          <span v-else>保存</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { getLlmSettings, updateLlmSettings, testLlmConnection } from '../api'

const props = defineProps({
  open: {
    type: Boolean,
    default: false,
  },
})

const emit = defineEmits(['close', 'saved'])

// 预设的 Base URL 列表
const presetBaseUrls = [
  'https://api.openai.com/v1',
  'https://api.deepseek.com/v1',
  'https://dashscope.aliyuncs.com/compatible-mode/v1',
  'http://localhost:11434/v1',
]

// 服务商预设配置
const providerPresets = {
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
const baseUrlToProvider = {
  'https://api.openai.com/v1': 'openai',
  'https://api.deepseek.com/v1': 'deepseek',
  'https://dashscope.aliyuncs.com/compatible-mode/v1': 'qwen',
  'http://localhost:11434/v1': 'ollama',
}

const formData = reactive({
  apiKey: '',
  baseUrl: 'https://api.openai.com/v1',
  model: 'gpt-4o',
})

const selectedProvider = ref('')
const customBaseUrl = ref('')
const customModel = ref('')
const originalApiKey = ref('')

const errors = reactive({
  apiKey: '',
  baseUrl: '',
  model: '',
})

const testing = ref(false)
const saving = ref(false)
const testResult = ref(null)

// 当前服务商
const currentProvider = computed(() => {
  return baseUrlToProvider[formData.baseUrl] || selectedProvider.value || ''
})

// 是否是预设的 Base URL
const isPresetBaseUrl = computed(() => {
  return presetBaseUrls.includes(formData.baseUrl)
})

const apiKeyPlaceholder = computed(() => {
  const provider = currentProvider.value
  if (provider && providerPresets[provider]) {
    return providerPresets[provider].apiKeyPlaceholder
  }
  return 'sk-...'
})

const apiKeyHint = computed(() => {
  const provider = currentProvider.value
  if (provider && providerPresets[provider]) {
    return providerPresets[provider].apiKeyHint
  }
  return '您的 API 密钥将安全保存'
})

// 实际使用的 Base URL
const actualBaseUrl = computed(() => {
  if (formData.baseUrl === 'custom') {
    return customBaseUrl.value
  }
  return formData.baseUrl
})

// 实际使用的 Model
const actualModel = computed(() => {
  if (formData.model === 'custom') {
    return customModel.value
  }
  return formData.model
})

const isFormValid = computed(() => {
  const baseUrl = actualBaseUrl.value
  const model = actualModel.value
  return (
    formData.apiKey.trim() !== '' &&
    baseUrl.trim() !== '' &&
    model.trim() !== '' &&
    !errors.apiKey &&
    !errors.baseUrl &&
    !errors.model
  )
})

// 服务商变化处理
const handleProviderChange = () => {
  if (selectedProvider.value && providerPresets[selectedProvider.value]) {
    const preset = providerPresets[selectedProvider.value]
    formData.baseUrl = preset.baseUrl
    formData.model = preset.model
    customBaseUrl.value = ''
    customModel.value = ''
  }
}

// Base URL 下拉框变化处理
const handleBaseUrlChange = () => {
  // 根据选择的 Base URL 更新服务商
  if (baseUrlToProvider[formData.baseUrl]) {
    selectedProvider.value = baseUrlToProvider[formData.baseUrl]
  }
  validateField('baseUrl')
}

const validateField = (field) => {
  errors[field] = ''

  if (field === 'apiKey' && !formData.apiKey.trim()) {
    errors.apiKey = 'API Key 不能为空'
  }

  if (field === 'baseUrl') {
    const url = actualBaseUrl.value
    if (!url.trim()) {
      errors.baseUrl = 'Base URL 不能为空'
    } else if (!url.startsWith('http')) {
      errors.baseUrl = '请输入有效的 URL'
    }
  }

  if (field === 'model') {
    const model = actualModel.value
    if (!model.trim()) {
      errors.model = 'Model 不能为空'
    }
  }
}

const loadSettings = async () => {
  try {
    const data = await getLlmSettings()
    const settings = data.items?.[0]
    if (settings) {
      const loadedUrl = settings.baseUrl || 'https://api.openai.com/v1'
      const loadedModel = settings.model || 'gpt-4o'

      // 判断是否是预设 URL
      if (presetBaseUrls.includes(loadedUrl)) {
        formData.baseUrl = loadedUrl
        selectedProvider.value = baseUrlToProvider[loadedUrl] || ''
      } else {
        formData.baseUrl = 'custom'
        customBaseUrl.value = loadedUrl
      }

      // 设置 Model
      formData.model = loadedModel
      originalApiKey.value = settings.apiKeyMasked || ''
    }
  } catch (error) {
    console.error('加载配置失败:', error)
  }
}

const handleTest = async () => {
  testing.value = true
  testResult.value = null

  try {
    const data = await testLlmConnection({
      apiKey: formData.apiKey,
      baseUrl: actualBaseUrl.value,
      model: actualModel.value,
    })

    const result = data.items?.[0] || data
    testResult.value = {
      success: true,
      message: result.message || '连接成功',
      responseTimeMs: result.responseTimeMs,
    }
  } catch (error) {
    testResult.value = {
      success: false,
      message: error.message || '连接测试失败',
    }
  } finally {
    testing.value = false
  }
}

const handleSave = async () => {
  if (!isFormValid.value) return

  saving.value = true

  try {
    await updateLlmSettings({
      apiKey: formData.apiKey,
      baseUrl: actualBaseUrl.value,
      model: actualModel.value,
    })

    emit('saved')
    emit('close')
  } catch (error) {
    testResult.value = {
      success: false,
      message: error.message || '保存失败',
    }
  } finally {
    saving.value = false
  }
}

// Reset form when dialog opens/closes
watch(
  () => props.open,
  (isOpen) => {
    if (isOpen) {
      loadSettings()
      testResult.value = null
    } else {
      // Clear API key when closing for security
      formData.apiKey = ''
      selectedProvider.value = ''
      customBaseUrl.value = ''
      customModel.value = ''
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

.dialog-body {
  padding: 24px;
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
