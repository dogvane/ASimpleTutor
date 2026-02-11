<template>
  <div v-if="open" class="settings-overlay" @click.self="$emit('close')">
    <div class="settings-dialog">
      <div class="dialog-header">
        <h2>LLM 配置设置</h2>
        <button type="button" class="close-btn" @click="$emit('close')">×</button>
      </div>

      <div class="dialog-body">
        <form @submit.prevent="handleSubmit" class="settings-form">
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
              placeholder="sk-..."
              :disabled="testing"
              @blur="validateField('apiKey')"
            />
            <span v-if="errors.apiKey" class="error">{{ errors.apiKey }}</span>
            <span class="hint">您的 API 密钥将安全保存</span>
          </div>

          <!-- Base URL -->
          <div class="form-group">
            <label for="baseUrl">
              Base URL
              <span class="required">*</span>
            </label>
            <input
              id="baseUrl"
              v-model="formData.baseUrl"
              type="url"
              placeholder="https://api.openai.com/v1"
              :disabled="testing"
              @blur="validateField('baseUrl')"
            />
            <span v-if="errors.baseUrl" class="error">{{ errors.baseUrl }}</span>
            <div class="presets">
              <button
                type="button"
                class="preset-btn"
                :class="{ active: formData.baseUrl === 'https://api.openai.com/v1' }"
                @click="formData.baseUrl = 'https://api.openai.com/v1'"
              >
                OpenAI
              </button>
              <button
                type="button"
                class="preset-btn"
                :class="{ active: formData.baseUrl === 'http://localhost:11434/v1' }"
                @click="formData.baseUrl = 'http://localhost:11434/v1'"
              >
                Ollama
              </button>
            </div>
          </div>

          <!-- Model -->
          <div class="form-group">
            <label for="model">
              Model
              <span class="required">*</span>
            </label>
            <input
              id="model"
              v-model="formData.model"
              type="text"
              placeholder="gpt-4"
              :disabled="testing"
              @blur="validateField('model')"
            />
            <span v-if="errors.model" class="error">{{ errors.model }}</span>
            <div class="presets">
              <button type="button" class="preset-btn" @click="formData.model = 'gpt-4'">
                GPT-4
              </button>
              <button type="button" class="preset-btn" @click="formData.model = 'gpt-3.5-turbo'">
                GPT-3.5
              </button>
            </div>
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

const formData = reactive({
  apiKey: '',
  baseUrl: 'https://api.openai.com/v1',
  model: 'gpt-4',
})

const originalApiKey = ref('')

const errors = reactive({
  apiKey: '',
  baseUrl: '',
  model: '',
})

const testing = ref(false)
const saving = ref(false)
const testResult = ref(null)

const isFormValid = computed(() => {
  return (
    formData.apiKey.trim() !== '' &&
    formData.baseUrl.trim() !== '' &&
    formData.model.trim() !== '' &&
    !errors.apiKey &&
    !errors.baseUrl &&
    !errors.model
  )
})

const validateField = (field) => {
  errors[field] = ''

  if (field === 'apiKey' && !formData.apiKey.trim()) {
    errors.apiKey = 'API Key 不能为空'
  }

  if (field === 'baseUrl') {
    if (!formData.baseUrl.trim()) {
      errors.baseUrl = 'Base URL 不能为空'
    } else if (!formData.baseUrl.startsWith('http')) {
      errors.baseUrl = '请输入有效的 URL'
    }
  }

  if (field === 'model' && !formData.model.trim()) {
    errors.model = 'Model 不能为空'
  }
}

const loadSettings = async () => {
  try {
    const data = await getLlmSettings()
    const settings = data.items?.[0]
    if (settings) {
      formData.baseUrl = settings.baseUrl || 'https://api.openai.com/v1'
      formData.model = settings.model || 'gpt-4'
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
      baseUrl: formData.baseUrl,
      model: formData.model,
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
      baseUrl: formData.baseUrl,
      model: formData.model,
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

.form-group input {
  border: 1px solid #d7dbe6;
  padding: 10px 12px;
  border-radius: 8px;
  font-size: 14px;
  transition: border-color 0.2s;
}

.form-group input:focus {
  outline: none;
  border-color: #3772ff;
  box-shadow: 0 0 0 3px rgba(55, 114, 255, 0.1);
}

.form-group input:disabled {
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

.presets {
  display: flex;
  gap: 8px;
  margin-top: 4px;
}

.preset-btn {
  border: 1px solid #d7dbe6;
  background: #fff;
  padding: 6px 12px;
  border-radius: 6px;
  font-size: 12px;
  cursor: pointer;
  transition: all 0.2s;
}

.preset-btn:hover {
  border-color: #3772ff;
  color: #3772ff;
}

.preset-btn.active {
  background: #3772ff;
  border-color: #3772ff;
  color: #fff;
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
