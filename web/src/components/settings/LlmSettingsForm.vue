<template>
  <form @submit.prevent="$emit('submit')" class="settings-form">
    <!-- 服务商选择 -->
    <div class="form-group">
      <label for="llm-provider">服务商</label>
      <select
        id="llm-provider"
        :value="selectedProvider"
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
      <label for="llm-apiKey">
        API Key
        <span class="required">*</span>
      </label>
      <input
        id="llm-apiKey"
        :value="formData.apiKey"
        type="password"
        :placeholder="apiKeyPlaceholder"
        :disabled="testing"
        @input="$emit('update:apiKey', $event.target.value)"
        @blur="$emit('validate', 'apiKey')"
      />
      <span v-if="errors.apiKey" class="error">{{ errors.apiKey }}</span>
      <span class="hint">{{ apiKeyHint }}</span>
    </div>

    <!-- Base URL -->
    <div class="form-group">
      <label for="llm-baseUrl">
        Base URL
        <span class="required">*</span>
      </label>
      <select
        id="llm-baseUrlSelect"
        :value="formData.baseUrl"
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
        id="llm-baseUrl"
        :value="customBaseUrl"
        type="url"
        placeholder="https://your-api-endpoint.com/v1"
        :disabled="testing"
        @input="$emit('update:customBaseUrl', $event.target.value)"
        @blur="$emit('validate', 'baseUrl')"
      />
      <span v-if="errors.baseUrl" class="error">{{ errors.baseUrl }}</span>
    </div>

    <!-- Model -->
    <div class="form-group">
      <label for="llm-model">
        Model
        <span class="required">*</span>
      </label>
      <select
        id="llm-model"
        :value="formData.model"
        :disabled="testing"
        @change="handleModelChange"
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
        id="llm-customModel"
        :value="customModel"
        type="text"
        placeholder="model-name"
        :disabled="testing"
        @input="$emit('update:customModel', $event.target.value)"
      />
      <span v-if="errors.model" class="error">{{ errors.model }}</span>
    </div>

    <!-- Test Connection -->
    <div class="form-group">
      <button
        type="button"
        class="test-btn"
        :disabled="testing || !isFormValid"
        @click="$emit('test')"
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
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  formData: {
    type: Object,
    required: true,
  },
  selectedProvider: {
    type: String,
    default: '',
  },
  customBaseUrl: {
    type: String,
    default: '',
  },
  customModel: {
    type: String,
    default: '',
  },
  errors: {
    type: Object,
    default: () => ({}),
  },
  testing: {
    type: Boolean,
    default: false,
  },
  testResult: {
    type: Object,
    default: null,
  },
  providerPresets: {
    type: Object,
    default: () => ({}),
  },
  presetBaseUrls: {
    type: Array,
    default: () => [],
  },
})

const emit = defineEmits([
  'update:apiKey',
  'update:customBaseUrl',
  'update:customModel',
  'providerChange',
  'baseUrlChange',
  'modelChange',
  'validate',
  'test',
  'submit',
])

const currentProvider = computed(() => props.selectedProvider)

const isPresetBaseUrl = computed(() => props.presetBaseUrls.includes(props.formData.baseUrl))

const apiKeyPlaceholder = computed(() => {
  const provider = props.selectedProvider
  if (provider && props.providerPresets[provider]) {
    return props.providerPresets[provider].apiKeyPlaceholder
  }
  return 'sk-...'
})

const apiKeyHint = computed(() => {
  const provider = props.selectedProvider
  if (provider && props.providerPresets[provider]) {
    return props.providerPresets[provider].apiKeyHint
  }
  return '您的 API 密钥将安全保存'
})

const isFormValid = computed(() => {
  const baseUrl = props.formData.baseUrl === 'custom' ? props.customBaseUrl : props.formData.baseUrl
  const model = props.formData.model === 'custom' ? props.customModel : props.formData.model
  return (
    props.formData.apiKey.trim() !== '' &&
    baseUrl.trim() !== '' &&
    model.trim() !== '' &&
    !props.errors.apiKey &&
    !props.errors.baseUrl &&
    !props.errors.model
  )
})

const handleProviderChange = (event) => {
  emit('providerChange', event.target.value)
}

const handleBaseUrlChange = (event) => {
  emit('baseUrlChange', event.target.value)
}

const handleModelChange = (event) => {
  emit('modelChange', event.target.value)
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
</style>
