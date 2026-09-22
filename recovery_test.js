import http from 'k6/http';
import { Rate } from 'k6/metrics';
import exec from 'k6/execution';

const p1_200 = new Rate('phase1_200');
const p1_429 = new Rate('phase1_429');

const p2_200 = new Rate('phase2_200');
const p2_429 = new Rate('phase2_429');

export const options = {
    scenarios: {
        phase1: { executor: 'constant-arrival-rate', rate: 3000, timeUnit: '1s', duration: '30s', preAllocatedVUs: 3000, maxVUs: 10000 },
        phase2: { executor: 'constant-arrival-rate', rate: 100, timeUnit: '1s', duration: '30s', preAllocatedVUs: 100, maxVUs: 1000, startTime: '30s' }
    },
    thresholds: {
        'http_req_duration{scenario:phase1}': ['p(95)>=0'],
        'http_req_duration{scenario:phase2}': ['p(95)>=0']
    }
};

export default function () {
    const url = 'http://localhost:5000/api/audit';
    const payload = JSON.stringify({
        message: 'recovery_test',
        priority: 'Normal',
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
    }
}
