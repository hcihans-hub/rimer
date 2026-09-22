import axios from 'axios';

const client = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 5000,
});

export const getSystemStatus = async () => {
  try {
    const response = await client.get('/system/status');
    return response.data;
  } catch (error) {
    console.error('Error fetching system status:', error);
    throw error;
  }
};

export const getRequestMetrics = async () => {
  try {
    const response = await client.get('/metrics/requests');
    return response.data;
  } catch (error) {
    console.error('Error fetching request metrics:', error);
    throw error;
  }
};

export const getQueueMetrics = async () => {
  try {
    const response = await client.get('/metrics/queue');
    return response.data;
  } catch (error) {
    console.error('Error fetching queue metrics:', error);
    throw error;
  }
};

export const getHealthMetrics = async () => {
  try {
    const response = await client.get('/system/health');
    return response.data;
  } catch (error) {
    console.error('Error fetching health metrics:', error);
    throw error;
  }
};

export const getQueueDetails = async () => {
  try {
    const response = await client.get('/metrics/queue/details');
    return response.data;
  } catch (error) {
    console.error('Error fetching queue details:', error);
    throw error;
  }
};

export const getEndpointMetrics = async () => {
  try {
    const response = await client.get('/metrics/endpoints');
    return response.data;
  } catch (error) {
    console.error('Error fetching endpoint metrics:', error);
    throw error;
  }
};

export const postProtectionMode = async (enabled) => {
  return await client.post('/system/protection-mode', { enabled });
};

export const postEmergencyDrop = async () => {
  return await client.post('/system/emergency-drop');
};

export const postSamplingRate = async (rate) => {
  return await client.post('/system/sampling-rate', { rate });
};

export default {
  getSystemStatus,
  getRequestMetrics,
  getQueueMetrics,
  getHealthMetrics,
  getQueueDetails,
  getEndpointMetrics,
  postProtectionMode,
  postEmergencyDrop,
  postSamplingRate,
};
