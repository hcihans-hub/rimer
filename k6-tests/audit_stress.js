import http from 'k6/http';

export const options = {
  scenarios: {
    audit_stress: {
      executor: 'ramping-arrival-rate',
      startRate: 200,
      timeUnit: '1s',
      preAllocatedVUs: 100,
      maxVUs: 5000,
      stages: [
        { target: 200, duration: '10s' },
        { target: 1000, duration: '0s' },
        { target: 1000, duration: '20s' },
        { target: 3000, duration: '0s' },
        { target: 3000, duration: '20s' }
      ],
    },
  },
};

export default function () {
  const url = 'http://localhost:5000/api/audit';
  const payload = JSON.stringify({
    message: 'load_test',
    priority: 'Low'
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  http.post(url, payload, params);
}
