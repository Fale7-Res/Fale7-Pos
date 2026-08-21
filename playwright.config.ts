import {defineConfig,devices} from '@playwright/test';
export default defineConfig({testDir:'./tests/e2e',fullyParallel:false,retries:0,use:{baseURL:'http://127.0.0.1:3100',trace:'retain-on-failure',...devices['Desktop Chrome'],channel:'chrome'},webServer:{command:'npm.cmd run dev -- -p 3100',url:'http://127.0.0.1:3100',reuseExistingServer:false,timeout:120000}});
