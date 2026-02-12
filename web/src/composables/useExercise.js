import { getExercises, submitExercise, submitFeedback } from '../api'

/**
 * 习题相关逻辑
 */
export function useExercise(state) {
  const {
    selectedKp,
    exercisesStatus,
    exercisesDrawerOpen,
    exercises,
    exercisesAnswers,
    exercisesFeedback,
    quizAnswers,
    quizFeedback,
    setError,
  } = state

  // 打开习题抽屉
  const openExercises = async () => {
    exercisesDrawerOpen.value = true
    if (exercisesStatus.value !== 'ready') return
    try {
      const data = await getExercises(selectedKp.value.id)
      exercises.value = data.items || []
      const answers = {}
      exercises.value.forEach((item) => {
        answers[item.id] = ''
      })
      exercisesAnswers.value = answers
      exercisesFeedback.value = {}
    } catch (error) {
      setError(error)
    }
  }

  // 关闭习题抽屉
  const closeExercises = () => {
    exercisesDrawerOpen.value = false
  }

  // 更新答案
  const updateAnswer = ({ id, value }) => {
    exercisesAnswers.value = { ...exercisesAnswers.value, [id]: value }
  }

  // 提交单个答案
  const submitOneAnswer = async (exerciseId) => {
    const answer = exercisesAnswers.value[exerciseId]
    try {
      const data = await submitExercise(exerciseId, answer)
      exercisesFeedback.value = {
        ...exercisesFeedback.value,
        [exerciseId]: data,
      }
      return data
    } catch (error) {
      setError(error)
      return null
    }
  }

  // 提交所有答案
  const submitAllAnswers = async () => {
    if (!selectedKp.value) return
    const answers = Object.entries(exercisesAnswers.value).map(([exerciseId, answer]) => ({
      exerciseId,
      answer,
    }))
    try {
      const data = await submitFeedback(selectedKp.value.id, answers)
      const feedbackMap = {}
      data.items?.forEach((item) => {
        feedbackMap[item.exerciseId] = item
      })
      exercisesFeedback.value = feedbackMap
    } catch (error) {
      setError(error)
    }
  }

  // 幻灯片习题相关
  const updateQuizAnswer = ({ exerciseId, value }) => {
    quizAnswers.value = { ...quizAnswers.value, [exerciseId]: value }
  }

  const submitQuizAnswer = async (event) => {
    const { exerciseId, answer } = event
    try {
      const data = await submitExercise(exerciseId, answer)
      quizFeedback.value = {
        ...quizFeedback.value,
        [exerciseId]: data,
      }
      return data
    } catch (error) {
      setError(error)
      return null
    }
  }

  return {
    openExercises,
    closeExercises,
    updateAnswer,
    submitOneAnswer,
    submitAllAnswers,
    updateQuizAnswer,
    submitQuizAnswer,
  }
}
