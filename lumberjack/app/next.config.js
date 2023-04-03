/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  resolve: {
    fallback: {
        "fs": false
    },
}
}

module.exports = nextConfig
