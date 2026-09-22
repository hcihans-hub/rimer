const autocannon = require('autocannon');

const token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjljNDBjZjMwLWRmM2EtNGJlNi04ZDZkLTczOGZmNTM4YmEyMCIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFkbWluIiwiZXhwIjoxNzc2NzI2ODg1LCJpc3MiOiJSaW1lckFwaSIsImF1ZCI6IlJpbWVyQXBpIn0.yVnImsv6YFv_I_YFv_I_YFv_I_YFv_I_YFv_I_YFv_I_YFv_I4";
const url = 'http://localhost:5000/api/tickets';

const instance = autocannon({
  url: url,
  connections: 50,
  duration: 60,
  rate: 400, // 400 RPS
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    title: "Stress Test",
    description: "Autocannon Verification",
    departmentId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    priority: 1
  })
}, (err, result) => {
  if (err) {
    console.error(err);
  } else {
    console.log(autocannon.format(result));
  }
});

process.on('SIGINT', () => {
  instance.stop();
});
