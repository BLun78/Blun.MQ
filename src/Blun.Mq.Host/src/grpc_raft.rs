use mq_proto::raft_rpc_server::RaftRpc;
use mq_proto::RaftEnvelope;
use mq_raft::Raft;
use tonic::{Request, Response, Status};

pub struct RaftRpcImpl {
    pub raft: Raft,
}

fn to_status<E: std::fmt::Debug>(err: E) -> Status {
    Status::internal(format!("{err:?}"))
}

#[tonic::async_trait]
impl RaftRpc for RaftRpcImpl {
    async fn append_entries(
        &self,
        request: Request<RaftEnvelope>,
    ) -> Result<Response<RaftEnvelope>, Status> {
        let req = serde_json::from_slice(&request.into_inner().payload).map_err(to_status)?;
        let resp = self.raft.append_entries(req).await.map_err(to_status)?;
        let payload = serde_json::to_vec(&resp).map_err(to_status)?;
        Ok(Response::new(RaftEnvelope { payload }))
    }

    async fn vote(&self, request: Request<RaftEnvelope>) -> Result<Response<RaftEnvelope>, Status> {
        let req = serde_json::from_slice(&request.into_inner().payload).map_err(to_status)?;
        let resp = self.raft.vote(req).await.map_err(to_status)?;
        let payload = serde_json::to_vec(&resp).map_err(to_status)?;
        Ok(Response::new(RaftEnvelope { payload }))
    }

    async fn install_snapshot(
        &self,
        request: Request<RaftEnvelope>,
    ) -> Result<Response<RaftEnvelope>, Status> {
        let req = serde_json::from_slice(&request.into_inner().payload).map_err(to_status)?;
        let resp = self.raft.install_snapshot(req).await.map_err(to_status)?;
        let payload = serde_json::to_vec(&resp).map_err(to_status)?;
        Ok(Response::new(RaftEnvelope { payload }))
    }
}
