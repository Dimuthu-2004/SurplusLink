import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const baseUrl = (__ENV.BASE_URL || 'http://localhost:5170').replace(/\/$/, '');
const readVus = Number(__ENV.READ_VUS || 10);
const readDuration = __ENV.READ_DURATION || '60s';
const matchRequirementId = __ENV.MATCH_REQUIREMENT_ID || '';
const measureWorkflow = (__ENV.MEASURE_WORKFLOW || 'false').toLowerCase() === 'true';

const apiRequests = new Counter('surpluslink_api_requests');
const apiSuccesses = new Rate('surpluslink_api_success_rate');
const materialsLatency = new Trend('materials_latency', true);
const requirementsLatency = new Trend('requirements_latency', true);
const matchEnqueueLatency = new Trend('match_enqueue_latency', true);
const workflowTotalLatency = new Trend('workflow_total_latency', true);

const matchScenario = matchRequirementId
  ? {
      match_generation: {
        executor: 'per-vu-iterations',
        vus: 1,
        iterations: 1,
        startTime: '2s',
        exec: 'generateMatch'
      }
    }
  : {};

export const options = {
  scenarios: {
    read_endpoints: {
      executor: 'constant-vus',
      vus: readVus,
      duration: readDuration,
      exec: 'readEndpoints'
    },
    ...matchScenario
  }
};

export function setup() {
  if (!__ENV.MATERIALS_TOKEN) throw new Error('MATERIALS_TOKEN is required');
  if (!__ENV.REQUIREMENTS_TOKEN) throw new Error('REQUIREMENTS_TOKEN is required');
  if (matchRequirementId && !__ENV.MATCH_TOKEN) throw new Error('MATCH_TOKEN is required');
  if (measureWorkflow && !__ENV.MANAGER_TOKEN) throw new Error('MANAGER_TOKEN is required');
  return {};
}

function headers(token) {
  return { headers: { Authorization: `Bearer ${token}` } };
}

function record(response, trend, route) {
  const successful = response.status >= 200 && response.status < 300;
  apiRequests.add(1, { route });
  apiSuccesses.add(successful, { route });
  trend.add(response.timings.duration);
  check(response, { [`${route} returned 2xx`]: () => successful });
  return successful;
}

export function readEndpoints() {
  if (__ITER % 2 === 0) {
    const response = http.get(`${baseUrl}/api/materials?page=1&pageSize=20`, {
      ...headers(__ENV.MATERIALS_TOKEN), tags: { route: 'GET /api/materials' }
    });
    record(response, materialsLatency, 'GET /api/materials');
  } else {
    const response = http.get(`${baseUrl}/api/requirements?page=1&pageSize=20`, {
      ...headers(__ENV.REQUIREMENTS_TOKEN), tags: { route: 'GET /api/requirements' }
    });
    record(response, requirementsLatency, 'GET /api/requirements');
  }
  sleep(Number(__ENV.THINK_TIME_SECONDS || 0));
}

export function generateMatch() {
  const started = Date.now();
  const response = http.post(
    `${baseUrl}/api/requirements/${matchRequirementId}/start-matching`, null,
    { ...headers(__ENV.MATCH_TOKEN), tags: { route: 'POST /api/requirements/{id}/start-matching' } }
  );
  const successful = record(response, matchEnqueueLatency, 'POST /api/requirements/{id}/start-matching');
  if (!successful || !measureWorkflow) return;

  const workflowId = response.json('workflowId');
  if (!workflowId) return check(response, { 'match response includes workflowId': () => false });

  const timeoutSeconds = Number(__ENV.WORKFLOW_TIMEOUT_SECONDS || 300);
  const pollSeconds = Number(__ENV.WORKFLOW_POLL_SECONDS || 2);
  const terminalStatuses = ['PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'FAILED', 'COMPLETED'];
  let status = '';
  for (let elapsed = 0; elapsed < timeoutSeconds; elapsed += pollSeconds) {
    sleep(pollSeconds);
    const poll = http.get(`${baseUrl}/api/workflows/${workflowId}`, {
      ...headers(__ENV.MANAGER_TOKEN), tags: { route: 'GET /api/workflows/{id}' }
    });
    const pollSuccessful = poll.status >= 200 && poll.status < 300;
    apiRequests.add(1, { route: 'GET /api/workflows/{id}' });
    apiSuccesses.add(pollSuccessful, { route: 'GET /api/workflows/{id}' });
    check(poll, { 'workflow poll returned 2xx': () => pollSuccessful });
    if (!pollSuccessful) continue;
    status = poll.json('status');
    if (terminalStatuses.includes(status)) break;
  }

  const completed = terminalStatuses.includes(status);
  check({ status }, { 'workflow reached a terminal status': () => completed });
  if (completed) workflowTotalLatency.add(Date.now() - started);
}

export function handleSummary(data) {
  return {
    stdout: textSummary(data),
    [__ENV.SUMMARY_FILE || 'perf/results/k6-summary.json']: JSON.stringify(data, null, 2)
  };
}

function textSummary(data) {
  const metrics = data.metrics;
  const lines = [
    'SurplusLink k6 performance run',
    `requests: ${metrics.surpluslink_api_requests?.values?.count ?? 'n/a'}`,
    `success rate: ${metrics.surpluslink_api_success_rate?.values?.rate ?? 'n/a'}`,
    `materials avg/p95 ms: ${metrics.materials_latency?.values?.avg ?? 'n/a'} / ${metrics.materials_latency?.values?.['p(95)'] ?? 'n/a'}`,
    `requirements avg/p95 ms: ${metrics.requirements_latency?.values?.avg ?? 'n/a'} / ${metrics.requirements_latency?.values?.['p(95)'] ?? 'n/a'}`,
    `match enqueue avg/p95 ms: ${metrics.match_enqueue_latency?.values?.avg ?? 'n/a'} / ${metrics.match_enqueue_latency?.values?.['p(95)'] ?? 'n/a'}`,
    `workflow total avg/p95 ms: ${metrics.workflow_total_latency?.values?.avg ?? 'n/a'} / ${metrics.workflow_total_latency?.values?.['p(95)'] ?? 'n/a'}`
  ];
  return `${lines.join('\n')}\n`;
}