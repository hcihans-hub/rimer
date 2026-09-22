import http from 'k6/http';
import { Rate } from 'k6/metrics';
import exec from 'k6/execution';

const p1_200 = new Rate('phase1_200');
const p1_429 = new Rate('phase1_429');

const p2_200 = new Rate('phase2_200');
const p2_429 = new Rate('phase2_429');

const p3_200 = new Rate('phase3_200');
const p3_429 = new Rate('phase3_429');

const p4_200 = new Rate('phase4_200');
const p4_429 = new Rate('phase4_429');

const p5_200 = new Rate('phase5_200');
const p5_429 = new Rate('phase5_429');

export const options = {
    scenarios: {
        phase1: { executor: 'constant-arrival-rate', rate: 100, timeUnit: '1s', duration: '10s', preAllocatedVUs: 100, maxVUs: 500 },
        phase2: { executor: 'constant-arrival-rate', rate: 500, timeUnit: '1s', duration: '10s', preAllocatedVUs: 500, maxVUs: 1500, startTime: '10s' },
        phase3: { executor: 'constant-arrival-rate', rate: 1000, timeUnit: '1s', duration: '10s', preAllocatedVUs: 1000, maxVUs: 3000, startTime: '20s' },
        phase4: { executor: 'constant-arrival-rate', rate: 2000, timeUnit: '1s', duration: '10s', preAllocatedVUs: 2000, maxVUs: 5000, startTime: '30s' },
        phase5: { executor: 'constant-arrival-rate', rate: 3000, timeUnit: '1s', duration: '10s', preAllocatedVUs: 3000, maxVUs: 8000, startTime: '40s' }
    },
    thresholds: {
        'http_req_duration{scenario:phase1}': ['p(95)>=0'],
        'http_req_duration{scenario:phase2}': ['p(95)>=0'],
        'http_req_duration{scenario:phase3}': ['p(95)>=0'],
        'http_req_duration{scenario:phase4}': ['p(95)>=0'],
        'http_req_duration{scenario:phase5}': ['p(95)>=0']
    }
};

export default function () {
    const url = 'http://localhost:5000/api/audit';
    const payload = JSON.stringify({
        message: 'break_test',
        priority: 'Low',
    });
    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.post(url, payload, params);
    
    const is200 = res.status === 200;
    const is429 = res.status === 429;

    const scenario = exec.scenario.name;

    if (scenario === 'phase1') {
        p1_200.add(is200);
        p1_429.add(is429);
    } else if (scenario === 'phase2') {
        p2_200.add(is200);
        p2_429.add(is429);
    } else if (scenario === 'phase3') {
        p3_200.add(is200);
        p3_429.add(is429);
    } else if (scenario === 'phase4') {
        p4_200.add(is200);
        p4_429.add(is429);
    } else if (scenario === 'phase5') {
        p5_200.add(is200);
        p5_429.add(is429);
    }
}
