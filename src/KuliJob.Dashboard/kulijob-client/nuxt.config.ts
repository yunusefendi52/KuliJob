import tailwindcss from "@tailwindcss/vite";
import { kill } from 'process'

const isDev = import.meta.env.NUXT_DEV ? true : false

export default defineNuxtConfig({
  compatibilityDate: '2025-05-15',
  devtools: { enabled: true },
  ssr: false,
  vite: {
    plugins: [
      tailwindcss(),
    ],
  },
  app: {
    baseURL: isDev ? '/kulijob' : undefined,
    cdnURL: !isDev ? './' : undefined,
  },
  css: [
    '~/assets/css/main.css',
  ],
  modules: [
    (_options, nuxt) => {
      if (!isDev) {
        nuxt.options.app.baseURL = './'
      }
    },
    '@nuxt/ui',
    '@nuxtjs/google-fonts',
    '@vueuse/nuxt',
    '@hebilicious/vue-query-nuxt',
  ],
  ui: {
    theme: {
    },
  },
  hooks: {
    'listen'(s, l) {
      let parentPid = 0
      function exitProcess() {
        if (parentPid) {
          kill(parentPid, 'SIGTERM')
        }
      }
      process.stdin.addListener('data', (d) => {
        const data = d.toString()
        if (data.startsWith('pid:')) {
          parentPid = Number.parseInt(data.replaceAll('pid:', ''))
          console.log('parent pid:', parentPid)
        }
      })
      process.stdin.addListener('error', (d) => {
        exitProcess()
      })
      process.stdin.addListener('close', (d) => {
        exitProcess()
      })
      process.stdin.addListener('end', () => {
        exitProcess()
      })
    },
  },
  vueQuery: {
    queryClientOptions: {
      defaultOptions: {
        queries: {
          refetchInterval: 5000,
        },
      },
    },
  },
  experimental: {
    // Disable automatic cache cleanup (default is true)
    purgeCachedData: false
  }
})
