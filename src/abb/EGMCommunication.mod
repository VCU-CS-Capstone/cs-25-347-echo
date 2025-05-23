MODULE EGMCommunication
    ! Author: Miles Popiela
    ! Email: popielamc@vcu.edu
    ! This file is not affiliated, sponsored
    ! or approved by ABB. Always refer to their application
    ! manual for more information.

    ! Identifier for EGM process
    VAR egmident egm_id;
    ! Current state of EGM process on controller
    VAR egmstate egm_state;

    ! Convergence criteria for translation and rotation (in degrees)
    CONST egm_minmax egm_minmax_translation := [-1, 1];
    CONST egm_minmax egm_minmax_rotation := [-1, 1];

    ! Correction frame for path correction
    LOCAL CONST pose egm_correction_frame := [[0, 0, 0], [1, 0, 0, 0]];
    LOCAL CONST pose egm_sensor_frame := [[0, 0, 0], [1, 0, 0, 0]];

    ! Main function
    PROC main()
        EGM_POSE_MOVEMENT;
    ENDPROC

    ! EGM function used to move the robot to a specific position using a pose target.
    ! In this approach, the external device sends a message to the robot specifying angles and a position
    ! in the cartesian coordinate system where the robot should move to.
    PROC EGM_POSE_MOVEMENT()
        ! Check if no EGM setup is active.
        IF egm_state = EGM_STATE_DISCONNECTED THEN
            TPWrite "EGM State: Preparing controller for EGM communication.";
        ENDIF

        WHILE TRUE DO
            ! Register a new EGM id.
            EGMGetId egm_id;

            ! Get current state of egm_id.
            egm_state := EGMGetState(egm_id);

            ! Setup the EGM communication.
            ! Make sure the external device name being used is the same specified in 
            ! the controller communication tab. In this example, the UdpUc device name is PC,
            ! moving the robot mechanical unit ROB_1.
            IF egm_state <= EGM_STATE_CONNECTED THEN
                EGMSetupUC ROB_1, egm_id, "default", "PC", \Pose; 
            ENDIF

            ! De-serializes the message sent by the external device.
            EGMActPose egm_id\Tool:=tool0, 
                       egm_correction_frame,
                       EGM_FRAME_BASE,
                       egm_sensor_frame,
                       EGM_FRAME_BASE
                       \x:=egm_minmax_translation
                       \y:=egm_minmax_translation
                       \z:=egm_minmax_translation
                       \rx:=egm_minmax_rotation
                       \ry:=egm_minmax_rotation
                       \rz:=egm_minmax_rotation
                       \LpFilter:= 16
                       \MaxSpeedDeviation:=100;

            ! Performs a movement based on the pose target sent by the external device.
            EGMRunPose egm_id, EGM_STOP_HOLD \x \y \z \rx \ry \rz \CondTime:=1\RampInTime:=0;

            ! (Debugging) Checks if robot is listening for external commands.
            IF egm_state = EGM_STATE_CONNECTED THEN
                TPWrite "EGM State: Waiting for movement request.";
            ENDIF

            ! (Debugging) Checks if the robot received an external command and is moving.
            IF egm_state = EGM_STATE_RUNNING THEN
                TPWrite "EGM State: Movement request received. Robot is moving.";
            ENDIF

            ! Reset EGM communication.
            IF egm_state <= EGM_STATE_CONNECTED THEN
                EGMReset egm_id;
            ENDIF
        ENDWHILE

        ! (Debugging) Checks if external devices are available.
        ERROR
        IF ERRNO = ERR_UDPUC_COMM THEN
            TPWrite "EGM Warning: Robot is not detecting any external devices.";
            TRYNEXT;
        ENDIF
    ENDPROC
ENDMODULE